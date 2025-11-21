-- Initial database schema
-- Run this in Supabase SQL Editor

-- Create boards table
CREATE TABLE boards (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now()
);

-- Create canvas_items table
CREATE TABLE canvas_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    board_id UUID NOT NULL REFERENCES boards(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    item_type TEXT NOT NULL CHECK (item_type IN ('note', 'image', 'link', 'todo')),
    position JSONB NOT NULL,
    size JSONB NOT NULL,
    content JSONB NOT NULL,
    metadata JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now()
);

-- Create indexes
CREATE INDEX idx_boards_user_id ON boards(user_id);
CREATE INDEX idx_boards_updated_at ON boards(updated_at DESC);
CREATE INDEX idx_canvas_items_board_id ON canvas_items(board_id);
CREATE INDEX idx_canvas_items_user_id ON canvas_items(user_id);
CREATE INDEX idx_canvas_items_updated_at ON canvas_items(updated_at DESC);