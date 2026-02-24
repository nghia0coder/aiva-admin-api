import http from "k6/http";
import { check, sleep } from "k6";
import { Trend, Counter, Rate } from "k6/metrics";

// ---------------------------
// Custom Metrics
// ---------------------------
const streamDuration = new Trend("stream_duration_ms", true);
const ttft = new Trend("time_to_first_token_ms", true);
const tokensReceived = new Counter("tokens_received");
const streamErrors = new Rate("stream_errors");
const requestRate = new Counter("total_requests");

// ---------------------------
// Load profile - SUSTAINED PEAK for Auto-Scale
// ---------------------------
export const options = {
    scenarios: {
        // Primary scenario: Long sustained peak
        sustained_peak_load: {
            executor: "ramping-vus",
            startVUs: 50,
            stages: [
                // Warm-up phase
                { duration: "1m", target: 150 },    // Gentle ramp-up
                { duration: "1m", target: 300 },    // Build pressure

                // PEAK PHASE - 10 minutes to ensure scaling triggers
                { duration: "10m", target: 500 },   // Sustained high load

                // Optional: Second peak to test scale-down and scale-up again
                { duration: "2m", target: 200 },    // Partial cool-down
                { duration: "8m", target: 600 },    // Second sustained peak

                // Cool-down
                { duration: "2m", target: 100 },
                { duration: "1m", target: 0 },
            ],
        },

        // Secondary scenario: Constant high request rate during peak
        constant_pressure: {
            executor: "constant-arrival-rate",
            rate: 80,                    // 80 requests per second
            timeUnit: "1s",
            duration: "20m",             // Match peak duration
            preAllocatedVUs: 150,
            maxVUs: 300,
            startTime: "2m",             // Start after warm-up
            gracefulStop: "30s",
        },
    },

    thresholds: {
        http_req_failed: ["rate<0.15"],           // Allow some failures under stress
        time_to_first_token_ms: ["p(95)<4000"],
        stream_duration_ms: ["p(95)<50000"],
        total_requests: ["count>8000"],           // Ensure enough requests
    },

    // Connection settings
    noConnectionReuse: false,
    userAgent: "k6-load-test/1.0",
};

// ---------------------------
// Config
// ---------------------------
const BASE_URL = __ENV.TARGET_URL || "https://your-app.azurewebsites.net";
const STREAM_ENDPOINT = "/mock/conversations";

const messages = [
    "Recommend a laptop for programming",
    "Best smartphone for photography",
    "Suggest a home office setup",
    "Best mechanical keyboard for developers",
    "Recommend travel gear for backpacking",
    "What monitor should I buy for coding?",
    "Explain quantum computing in simple terms",
    "Best practices for microservices architecture",
    "How to optimize database queries?",
    "What's the future of AI assistants?",
    "Compare cloud providers for startups",
    "Best security practices for web apps",
];

// ---------------------------
// Virtual User behavior
// ---------------------------
export default function () {
    const conversationId = uuidv4();
    const message = messages[Math.floor(Math.random() * messages.length)];

    const payload = JSON.stringify({
        message: message,
        userName: `user_${__VU}_${__ITER}`,
    });

    const url = `${BASE_URL}${STREAM_ENDPOINT}/${conversationId}/stream`;

    const params = {
        headers: {
            "Content-Type": "application/json",
            Accept: "text/event-stream",
            "Cache-Control": "no-cache",
            Connection: "keep-alive",
        },
        timeout: "60s",
        tags: { name: "ai_stream_request" },
    };

    const startTime = Date.now();
    let firstTokenTime = null;
    let tokenCount = 0;

    try {
        const res = http.post(url, payload, params);

        requestRate.add(1);

        const statusOk = check(res, {
            "status 200": (r) => r.status === 200,
            "has content-type": (r) => r.headers && r.headers["Content-Type"],
        });

        if (res.status !== 200) {
            if (__ITER % 100 === 0) {
                console.log(`[VU ${__VU}] Request failed: ${res.status}`);
            }
            streamErrors.add(1);
            return;
        }

        const body = res.body || "";
        const lines = body.split("\n");

        for (const line of lines) {
            if (line.startsWith("data:")) {
                const data = line.replace("data:", "").trim();
                if (data === "[DONE]") break;

                tokenCount++;

                if (!firstTokenTime) {
                    firstTokenTime = Date.now();
                    ttft.add(firstTokenTime - startTime);
                }
            }
        }

        tokensReceived.add(tokenCount);
        streamDuration.add(Date.now() - startTime);
        streamErrors.add(0);

    } catch (err) {
        if (__ITER % 100 === 0) {
            console.error(`[VU ${__VU}] Exception: ${err.message}`);
        }
        streamErrors.add(1);
    }

    // Fast iteration to maintain high CPU
    sleep(Math.random() * 0.15 + 0.05); // 0.05-0.2s between requests
}

// ---------------------------
// Helpers
// ---------------------------
function uuidv4() {
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0;
        const v = c === "x" ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

// ---------------------------
// Lifecycle hooks for monitoring
// ---------------------------
export function handleSummary(data) {
    console.log("\n========================================");
    console.log("LOAD TEST SUMMARY");
    console.log("========================================");
    console.log(`Total Requests: ${data.metrics.total_requests.values.count}`);
    console.log(`Request Rate: ${(data.metrics.total_requests.values.count / data.state.testRunDurationMs * 1000).toFixed(2)} req/s`);
    console.log(`Failed Requests: ${(data.metrics.http_req_failed.values.rate * 100).toFixed(2)}%`);
    console.log(`Avg TTFT: ${data.metrics.time_to_first_token_ms.values.avg.toFixed(2)}ms`);
    console.log(`P95 TTFT: ${data.metrics.time_to_first_token_ms.values['p(95)'].toFixed(2)}ms`);
    console.log("========================================\n");

    return {
        'summary.json': JSON.stringify(data, null, 2),
    };
}