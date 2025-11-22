# Coding Conventions

## Project Organization
- Features are organized by vertical slice (loose Vertical Slice Architecture)
- Each feature has its own Pages, Components, Services, and Models
- Shared components and services go in the Shared/ folder

## File Naming
- Use PascalCase for C# files: `BoardService.cs`
- Use kebab-case for JavaScript files: `canvas-interop.js`
- Use PascalCase for Razor components: `BoardList.razor`

## C# Conventions
- Follow standard C# coding conventions
- Use nullable reference types
- Keep files small and focused (single responsibility)
- Use dependency injection for services
- Async/await for all I/O operations

## JavaScript Conventions
- Use ES6 modules
- Export functions explicitly
- Keep files small and focused
- Use descriptive function names
- Document JSInterop contracts

## Component Structure
- Razor components should have clear render modes specified
- Separate concerns: UI logic vs business logic
- Use services for data access and business logic
- Keep components focused on presentation