# Database Migrations

This directory contains SQL migration scripts for the Supabase PostgreSQL database.

## How to Run Migrations

1. Log into your Supabase project dashboard
2. Navigate to **SQL Editor**
3. Run migrations in order (001, 002, 003, 004, etc.)
4. Verify each migration completes successfully before proceeding

## Migration List

### 001_initial_schema.sql
Creates the core database tables:
- `boards` - User boards/workspaces
- `board_items` - Canvas items (notes, images, links, todos)
- Indexes for performance

**Status**: ⚠️ Incomplete - Missing UUID/timestamp defaults (fixed in 004)

### 002_rls_policies.sql
Sets up Row-Level Security (RLS) policies:
- Ensures users can only access their own boards and items
- Required for multi-tenant security

### 003_storage_setup.sql
Configures Supabase Storage:
- Sets up buckets for user uploads
- Configures access policies

### 004_add_uuid_and_timestamp_defaults.sql ⭐ **RUN THIS NOW**
Fixes missing defaults from migration 001:
- Adds `DEFAULT gen_random_uuid()` to `id` columns
- Adds `DEFAULT now()` to `created_at` and `updated_at` columns
- Creates trigger to auto-update `updated_at` on row changes

**IMPORTANT**: If you've already run migrations 001-003, you MUST run this migration to fix board creation issues.

## Current Issue: Missing UUID Defaults

If you see board IDs showing as `00000000-0000-0000-0000-000000000000`, it means migration 004 hasn't been applied yet.

**To fix:**
1. Go to Supabase SQL Editor
2. Copy and paste the contents of `004_add_uuid_and_timestamp_defaults.sql`
3. Click "Run"
4. Verify the migration succeeds
5. Test board creation again

## Verification

After running migration 004, verify it worked:

```sql
-- Check that boards table has UUID default
SELECT column_name, column_default
FROM information_schema.columns
WHERE table_name = 'boards' AND column_name = 'id';
-- Should show: gen_random_uuid()

-- Check that triggers exist
SELECT trigger_name, event_manipulation, event_object_table
FROM information_schema.triggers
WHERE event_object_table IN ('boards', 'board_items');
-- Should show update triggers for both tables
```

## Best Practices

- Always backup your database before running migrations
- Test migrations in a development environment first
- Run migrations during low-traffic periods
- Keep migrations idempotent when possible (using IF NOT EXISTS, DROP IF EXISTS, etc.)
