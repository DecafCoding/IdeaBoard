-- Enable RLS on tables
ALTER TABLE boards ENABLE ROW LEVEL SECURITY;
ALTER TABLE canvas_items ENABLE ROW LEVEL SECURITY;

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

-- Canvas items policies
CREATE POLICY "Users can view their own canvas items"
    ON canvas_items FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can create their own canvas items"
    ON canvas_items FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own canvas items"
    ON canvas_items FOR UPDATE
    USING (auth.uid() = user_id);

CREATE POLICY "Users can delete their own canvas items"
    ON canvas_items FOR DELETE
    USING (auth.uid() = user_id);