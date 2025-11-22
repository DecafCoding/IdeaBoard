-- Initial database schema
-- Run this in Supabase SQL Editor

-- Create boards table
CREATE TABLE boards (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    name TEXT NOT NULL,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- Create board_items table
CREATE TABLE board_items (
    id UUID PRIMARY KEY,
    board_id UUID NOT NULL,
    user_id UUID NOT NULL,
    item_type TEXT NOT NULL, -- 'note', 'image', 'link', 'todo'
    position JSONB NOT NULL, -- {x, y, z_index}
    size JSONB NOT NULL, -- {width, height}
    content JSONB NOT NULL, -- Type-specific content
    metadata JSONB, -- Color, formatting, etc.
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);

-- Create indexes
CREATE INDEX idx_boards_user_id ON boards(user_id);
CREATE INDEX idx_boards_updated_at ON boards(updated_at DESC);
CREATE INDEX idx_board_items_board_id ON board_items(board_id);
CREATE INDEX idx_board_items_user_id ON board_items(user_id);
CREATE INDEX idx_board_items_updated_at ON board_items(updated_at DESC);