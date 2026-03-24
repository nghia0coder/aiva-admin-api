# Stream Chat API with Image Support

## Overview
The Stream Chat endpoint (`/conversations/{conversationId}/stream`) supports real-time AI responses with image upload capabilities.

## Features
- **Text Messages**: Send text-based queries to the AI assistant
- **Image Upload**: Upload up to 5 images (max 10MB each) for visual context
- **Intent Routing**: Automatically routes to Data Assistant (Admins) or Shopping Assistant (Customers)
- **Real-time Streaming**: Server-sent events for immediate response streaming

## Supported Image Types
- JPEG (`.jpg`, `.jpeg`)
- PNG (`.png`)
- BMP (`.bmp`)
- GIF (`.gif`)
- WebP (`.webp`)

## Request Format

### Text Only Request
```javascript
const formData = new FormData();
formData.append('Message', 'What products do you recommend for winter clothing?');
formData.append('ConversationId', conversationId);

fetch('/conversations/{conversationId}/stream', {
  method: 'POST',
  body: formData
});
```

### Text + Image Request
```javascript
const formData = new FormData();
formData.append('Message', 'I like this style, can you recommend similar products?');
formData.append('ConversationId', conversationId);
formData.append('HasImages', 'true');

// Add images
for (let i = 0; i < imageFiles.length; i++) {
  formData.append('images', imageFiles[i]);
}

fetch('/conversations/{conversationId}/stream', {
  method: 'POST',
  body: formData
});
```

## Response Events

### Success Response
```javascript
const eventSource = new EventSource('/conversations/{conversationId}/stream');

eventSource.addEventListener('message', (event) => {
  const data = JSON.parse(event.data);
  console.log('AI Response:', data.content);
  console.log('Processed Images:', data.processedImages);
  console.log('Image Count:', data.imageCount);
});

eventSource.addEventListener('action', (event) => {
  const data = JSON.parse(event.data);
  console.log('Action:', data.actionType, data.payload);
});

eventSource.addEventListener('done', (event) => {
  console.log('Stream completed');
  eventSource.close();
});
```

### Error Response
```javascript
eventSource.addEventListener('error', (event) => {
  const data = JSON.parse(event.data);
  console.error('Error:', data.message);
});
```

## Validation Rules
- Maximum 5 images per request
- Maximum 10MB per image
- Only supported image formats accepted
- Images are processed for shopping context (products, brands, features)

## Use Cases

### Shopping Assistant (Customers)
- Product identification from images
- Style matching and recommendations
- Brand recognition from photos
- Visual search and comparison

### Data Assistant (Admins)
- Currently text-only (image support may be added in future)
- SQL query generation and execution
- Data analysis and reporting

## Error Handling
Common error scenarios:
- `"Too many images. Maximum allowed: 5"`
- `"Image 'filename.jpg' is too large. Maximum size: 10MB"`
- `"User not authenticated"`
- `"Conversation not found"`
- `"Unsupported file type"`