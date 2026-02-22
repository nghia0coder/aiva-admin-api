# Tool Selection Rules

Use these rules to decide **when** and **which** tool to call. Apply in priority order. If no tool matches, respond with text only.

---

## 1. add_to_cart

**Purpose:** Add a product to the user's shopping cart.

**When to call:**
- User clearly wants to add product(s) to cart.
- Trigger phrases: "thêm vào giỏ", "add to cart", "mua cái này", "cho tôi", "đặt hàng", "add this", "buy this", "order", v.v.

**Input required:**
- `product_id` (required) – Product ID from catalog or selection data.
- `quantity` (optional, default: 1).
- `size`, `color` (optional) – when product has configurable attributes.

**Flow A – [product_data] exists with SELECTED items:**
- Use ProductId and Quantity directly from `[product_data]`.
- Call `add_to_cart` for each SELECTED product with their specified quantities.
- Do NOT call `search_infors` or `get_product_info` first.

**Flow B – No [product_data]:**
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
- Need to resolve product name to ProductId before add_to_cart (when no [product_data]).

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
- [product_data] exists with UNCHECKED products → remove those items.

**Input:**
- `product_id` (required).
- `quantity` (optional, default: remove all).

---

## 5. No tool – text response only

**When to use:**
- Greetings: "xin chào", "hello", "hi".
- General questions that do not require tool data.
- Need clarification before acting.
- Multiple tools could apply and user intent is ambiguous.

---

## Placeholder reference

| Placeholder     | Description |
|-----------------|-------------|
| `[product_data]`| User-selected products from table (ProductId, Quantity, SELECTED/UNCHECKED status). |
| `[user_query]`  | Current user message. |
| `[retrieved_context]` | Product/catalog info from search. |
| `[additional_data]`   | Tool execution results, cart updates, etc. |

---

## Summary

- **add_to_cart**: Call when intent is clear and ProductId is known (from [product_data] or resolved via search).
- **search_infors**: Call when user is searching or when ProductId must be resolved.
- **get_product_info**: Call when user needs product details or ProductId validation.
- **remove_from_cart**: Call when user clearly wants to remove items.
- **No tool**: Respond with text when no tool applies or more clarification is needed.
