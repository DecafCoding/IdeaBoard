# Visual Canvas Workspace

A Blazor-based visual workspace application for organizing ideas, research, and projects on an infinite canvas.

## Features (Planned MVP)
-  Infinite canvas with pan and zoom
-  Text notes with formatting
-  Image uploads
-  Link cards with previews
-  To-do items
-  Auto-save
-  User authentication

## Tech Stack
- .NET 9 Blazor (Hybrid: Server + WebAssembly)
- Supabase (Auth, Database, Storage)
- Panzoom.js for canvas interactions
- Tailwind-inspired CSS (utility classes)

## Project Structure
```
src/
├── VisualCanvasWorkspace/          # Main Blazor app
│   ├── Features/                   # Feature modules
│   │   ├── Authentication/
│   │   ├── Boards/
│   │   └── Canvas/
│   ├── Shared/                     # Shared components/services
│   └── wwwroot/                    # Static assets
└── VisualCanvasWorkspace.Shared/   # Shared library (models, constants)
```

## Getting Started

See [docs/development/setup.md](docs/development/setup.md) for detailed setup instructions.

### Quick Start
1. Install .NET 9 SDK
2. Clone this repository
3. Set up Supabase project and credentials
4. Run database migrations
5. `dotnet run --project src/VisualCanvasWorkspace`

## Development Phases

### ✅ Phase 0: Foundation (Current)
- Project structure
- Supabase setup
- Basic authentication

### 🚧 Phase 1: Authentication & Boards
- User registration/login
- Board CRUD operations

### ⏳ Phase 2: Canvas Core
- Canvas with pan/zoom
- Basic item rendering
- Drag and drop

### ⏳ Phase 3: Content Types
- Text notes
- Images
- Links
- To-dos

### ⏳ Phase 4: Polish
- Error handling
- Loading states
- Responsive design

## Architecture

This project uses a **hybrid Blazor approach**:
- Server rendering for auth, navigation, board list
- WebAssembly rendering for canvas (performance-critical)
- 70% C# (business logic) / 30% JavaScript (interactions)

See [docs/architecture/decisions.md](docs/architecture/decisions.md) for detailed architecture decisions.

## Contributing

This is currently a solo development project following the product backlog in `docs/MVP_Product_Backlog.md`.

## License

[Choose your license]