# Architecture Decision Records

## ADR-001: Hybrid Blazor (Server + WebAssembly)
**Status**: Accepted  
**Context**: Need balance between development speed and performance  
**Decision**: Use Auto render mode with strategic WebAssembly for canvas  
**Consequences**: 
- Fast development with Server rendering
- High performance for canvas interactions
- Slightly larger initial payload

## ADR-002: Feature-Based Organization
**Status**: Accepted  
**Context**: Need maintainable structure as app grows  
**Decision**: Organize by feature (loose Vertical Slice)  
**Consequences**:
- Easy to locate related code
- Clear feature boundaries
- Easier to work on features independently

## ADR-003: 70/30 C#/JavaScript Split
**Status**: Accepted  
**Context**: Balance between business logic safety and interaction performance  
**Decision**: 70% C# (state, logic), 30% JavaScript (interactions)  
**Consequences**:
- Business logic in type-safe C#
- Smooth 60fps canvas interactions
- Clear boundaries via JSInterop

## ADR-004: Supabase for Backend
**Status**: Accepted  
**Context**: Need backend services without building custom API  
**Decision**: Use Supabase for auth, database, and storage  
**Consequences**:
- Fast development
- Built-in auth and RLS
- Vendor lock-in

## ADR-005: Canvas Component Render Mode
Context: Canvas requires high-performance 60fps interactions for drag, pan, and zoom operations
Decision: Use @rendermode InteractiveWebAssembly for Canvas component and all child components

## ADR-006: Image Handling Workflow

Upload Flow:
- Client-side validation (type, size)
- Generate filename as {UUID}.{extension}
- Upload to Supabase Storage at {user_id}/{board_id}/{item_id}.ext
- Get public URL and create canvas item
- Display with CSS sizing

Constraints:
- Max file size: 3MB
- Accepted formats: JPEG, PNG, GIF, WebP
- Max display dimensions: 1200×1200px (CSS)
- No client-side compression
- No format conversion
- Immediate deletion on item remove

User Feedback:
- Percentage-based progress bar during upload
- Manual retry on failure

Storage Structure:
canvas-images/
  {user_id}/
    {board_id}/
      {item_id}.jpg
      {item_id}.png

## ADR-007: Authentication Flow

**Status**: Accepted  
**Date**: 2024-11-23

### Token Storage
**Server components**: HTTP-only secure cookie  
**WebAssembly components**: localStorage (base64 encoded)  
**Synchronization**: CustomAuthStateProvider manages both stores

### Token Lifetimes
* Access token: 1 hour
* Refresh token (Remember Me): 30 days
* Refresh token (Default): 7 days

### Token Refresh Strategy
* **Proactive**: Background timer refreshes at 50 minutes
* **Reactive**: HTTP interceptor handles 401 responses
* **Implementation**: C# AuthService with System.Threading.Timer

### Authentication State Provider
* **Single provider**: `CustomAuthStateProvider` with render mode detection
* **Token retrieval**: DI-injected `ITokenStorage` interface (Cookie or LocalStorage implementation)
* **State updates**: `NotifyAuthenticationStateChanged()` on login/logout/refresh

### Session Management
* **Concurrent sessions**: Allowed
* **Idle timeout**: None for MVP
* **Multi-device**: Supported (same user, multiple devices)

### Authorization Header Injection
* **Server**: HttpClient DelegatingHandler (`AuthHeaderHandler`)
* **WebAssembly**: SupabaseService constructor injects header from localStorage
* **Implementation**: Both use `ITokenStorage.GetTokenAsync()`

### Logout Behavior
* Clear both cookie and localStorage
* **Unsaved canvas changes**: Lost (show warning modal)
* **Viewport state**: Preserved in localStorage (board-specific)
* **Redirect to**: `/login`

### Error Handling
* **Invalid credentials**: Inline error message, no redirect
* **Network failure**: Retry once automatically, then show error
* **Expired token (no refresh)**: Redirect to `/login` with return URL
* **Token refresh failure**: Clear auth state, redirect to `/login`

### Security Measures (MVP)
* **CSRF**: Not required (Supabase handles token validation)
* **Rate limiting**: Rely on Supabase built-in limits
* **Email verification**: Disabled for MVP
* **Password reset**: Deferred to post-MVP
* **Password requirements**: Min 8 characters (Supabase default)

### Implementation Order
1. Token storage interfaces and implementations
2. AuthService with login/register/refresh methods
3. CustomAuthStateProvider
4. AuthHeaderHandler (DelegatingHandler)
5. Protected routes configuration
6. Logout functionality
