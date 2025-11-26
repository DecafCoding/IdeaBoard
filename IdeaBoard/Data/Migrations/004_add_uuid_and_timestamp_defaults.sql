-- Migration 004: Add UUID and Timestamp Defaults
-- Run this in Supabase SQL Editor
-- This fixes boards and board_items to auto-generate IDs and timestamps

-- Add default UUID generation for boards table
ALTER TABLE boards
    ALTER COLUMN id SET DEFAULT gen_random_uuid(),
    ALTER COLUMN created_at SET DEFAULT now(),
    ALTER COLUMN updated_at SET DEFAULT now();

-- Add default UUID generation for board_items table
ALTER TABLE board_items
    ALTER COLUMN id SET DEFAULT gen_random_uuid(),
    ALTER COLUMN created_at SET DEFAULT now(),
    ALTER COLUMN updated_at SET DEFAULT now();

-- Create trigger function to auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Add trigger to boards table
DROP TRIGGER IF EXISTS update_boards_updated_at ON boards;
CREATE TRIGGER update_boards_updated_at
    BEFORE UPDATE ON boards
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Add trigger to board_items table
DROP TRIGGER IF EXISTS update_board_items_updated_at ON board_items;
CREATE TRIGGER update_board_items_updated_at
    BEFORE UPDATE ON board_items
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();
