-- Enable RLS on tables
ALTER TABLE boards ENABLE ROW LEVEL SECURITY;
ALTER TABLE board_items ENABLE ROW LEVEL SECURITY;

-- Boards policies
CREATE POLICY "Users can view their own boards"
    ON boards FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can create their own boards"
    ON boards FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own boards"
    ON boards FOR UPDATE
    USING (auth.uid() = user_id);

CREATE POLICY "Users can delete their own boards"
    ON boards FOR DELETE
    USING (auth.uid() = user_id);

-- Board items policies
CREATE POLICY "Users can view their own board items"
    ON board_items FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can create their own board items"
    ON board_items FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own board items"
    ON board_items FOR UPDATE
    USING (auth.uid() = user_id);

CREATE POLICY "Users can delete their own board items"
    ON board_items FOR DELETE
    USING (auth.uid() = user_id);