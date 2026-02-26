# Tool Selection Rules

Use these rules to decide **when** and **which** tool to call. Apply in priority order. If no tool matches, respond with text only.

**User selected products (use for add_to_cart/remove_from_cart):**
[additional_data]

---

## 1. add_to_cart

**Purpose:** Add a product to the user's shopping cart.

**When to call:**
- User clearly wants to add product(s) to cart.
- Trigger phrases: "thêm vào giỏ", "add to cart", "mua cái này", "cho tôi", "đặt hàng", "add this", "buy this", "order", v.v.

**Input required:**
- `product_id` (required) – Product ID from catalog or selection data.
- `product_name` (required) – Product name for display.
- `quantity` (optional, default: 1).
- `search_attributes` (optional) – Array of `{ name, value }` from `[additional_data]` item's `extraData.searchAttributes`. Use when product has configurable attributes (Color, Size, Memory capacity, Material, etc.).
- `size`, `color` (optional, legacy) – fallback when search_attributes not used.

**Flow A – [additional_data] exists with selected products:**
- [additional_data] is a JSON array: `[{ productId, quantity, extraData: { searchAttributes: [{ name, value }] } }]`
- Call `add_to_cart` for EACH item in the array.
- Use `productId` → `product_id`, `quantity` → `quantity`, `extraData.searchAttributes` → `search_attributes`.
- Resolve `product_name` from product info if not in data, or use "Product #{productId}".
- Do NOT call `search_infors` or `get_product_info` first.

**Flow B – No [additional_data]:**
1. User mentions a specific product by name or ID.
2. Call `search_infors` or `get_product_info` to resolve ProductId.
3. If exactly 1 match → call `add_to_cart` with that ProductId.
4. If multiple matches → do NOT call `add_to_cart`. Respond with a list and ask user to choose.
5. If no match → respond "No matching product found". Do not guess.

**When NOT to call:**
- Multiple products match and user has not chosen.
- ProductId cannot be determined.
- User is only browsing or asking questions, not adding to cart.
- Never guess or invent ProductId.

---

## 2. search_infors

**Purpose:** Search for products or information about shop store by query or filters.

**When to call:**
- User wants to find products or any information: "tìm", "search", "có sản phẩm", "giới thiệu", "tôi cần", "looking for", v.v.
- Need to resolve product name to ProductId before add_to_cart (when no [additional_data]).

**Input:**
- `query` (required) – search terms.
- `category`, `price_min`, `price_max` (optional).

---

## 3. get_product_info

**Purpose:** Get detailed information about a specific product.

**When to call:**
- User asks for details of a specific product: "chi tiết sản phẩm", "thông tin sản phẩm", "product info", "details of product X".
- Need to validate ProductId or get attributes before add_to_cart.

**Input:**
- `product_id` (required).

---

## 4. remove_from_cart

**Purpose:** Remove a product from the user's shopping cart.

**When to call:**
- User wants to remove item(s): "xóa khỏi giỏ", "remove", "bỏ", "delete from cart", v.v.
- [additional_data] exists with products user wants to remove → remove those items.

**Input:**
- `product_id` (required).
- `quantity` (optional, default: remove all).

---

## 5. get_cart

**Purpose:** Returns the user's shopping cart with full product details (name, price, URL, quantity, subtotal).

**When to call:**
- User asks about cart: "cart", "cart info", "what's in my cart", "giỏ hàng", "xem giỏ", etc.

**Returns (to LLM):**
- Pre-merged, formatted cart data: ProductName, BasePrice, full URL, Quantity, Subtotal, RawAttributes (or "Attribute <Key>: [ValueIds]" if not mappable).
- Products not found are marked "Not found".
- Empty cart returns a simple "Your cart is empty" message.

**Input:**
- No parameters required (returns current user's cart).

**Note:** This tool resolves product details internally. Do NOT call search_infors or get_product_info after get_cart — the response is already complete.

---

## 6. checkout

**Purpose:** Initiate checkout process and redirect user to checkout page.

**When to call:**
- User explicitly wants to checkout or complete their purchase.
- Trigger phrases: "checkout", "thanh toán", "đặt hàng xong", "complete order", "proceed to checkout", "mua luôn", "pay now", v.v.
- User has items in cart and confirms they want to buy.

**Input:**
- No parameters required.

**What it does:**
- Prepares checkout session.
- Returns checkout URL (https://smartstore-demo-bnf3hzhpdvbkabad.southeastasia-01.azurewebsites.net) for frontend to redirect.
- Frontend will automatically redirect user to checkout page.

**When NOT to call:**
- User is still browsing or adding items.
- User hasn't confirmed purchase intent.
- Cart is empty (should prompt user to add items first).

**Return:**
- Success message with checkout URL for redirect action.

---

## 7. No tool – text response only

**When to use:**
- Greetings: "xin chào", "hello", "hi".
- General questions that do not require tool data.
- Need clarification before acting.
- Multiple tools could apply and user intent is ambiguous.

---

## Placeholder reference

| Placeholder     | Description |
|-----------------|-------------|
| `[additional_data]` | User-selected products: `[{ productId, quantity, extraData: { searchAttributes: [{ name, value }] } }]`. Use for add_to_cart/remove_from_cart. |
| `[user_query]`  | Current user message. |
| `[retrieved_context]` | Product/catalog info from search. |

---

## Summary

- **add_to_cart**: Call when intent is clear and ProductId is known (from [additional_data] or resolved via search). Pass search_attributes from extraData.searchAttributes when available.
- **search_infors**: Call when user is searching or when ProductId must be resolved.
- **get_product_info**: Call when user needs product details or ProductId validation.
- **remove_from_cart**: Call when user clearly wants to remove items.
- **GetEcomCartWS**: Call when user asks about cart/wishlist. Merge with search_infors/get_product_info to show ProductName, price, URL.
- **checkout**: Call when user explicitly wants to checkout/complete purchase. Returns redirect URL.
- **No tool**: Respond with text when no tool applies or more clarification is needed.
