# Architecture Decision Records

## ADR-001: Hybrid Blazor (Server + WebAssembly)
**Status**: Accepted  
**Context**: Need balance between development speed and performance  
**Decision**: Use Auto render mode with strategic WebAssembly for canvas  
**Consequences**: 
- ✅ Fast development with Server rendering
- ✅ High performance for canvas interactions
- ⚠️ Slightly larger initial payload

## ADR-002: Feature-Based Organization
**Status**: Accepted  
**Context**: Need maintainable structure as app grows  
**Decision**: Organize by feature (loose Vertical Slice)  
**Consequences**:
- ✅ Easy to locate related code
- ✅ Clear feature boundaries
- ✅ Easier to work on features independently

## ADR-003: 70/30 C#/JavaScript Split
**Status**: Accepted  
**Context**: Balance between business logic safety and interaction performance  
**Decision**: 70% C# (state, logic), 30% JavaScript (interactions)  
**Consequences**:
- ✅ Business logic in type-safe C#
- ✅ Smooth 60fps canvas interactions
- ✅ Clear boundaries via JSInterop

## ADR-004: Supabase for Backend
**Status**: Accepted  
**Context**: Need backend services without building custom API  
**Decision**: Use Supabase for auth, database, and storage  
**Consequences**:
- ✅ Fast development
- ✅ Built-in auth and RLS
- ⚠️ Vendor lock-in