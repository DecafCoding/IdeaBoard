CREATE POLICY "Users can upload their own images"
    ON storage.objects FOR INSERT
    WITH CHECK (
        bucket_id = 'canvas-images' AND
        auth.uid()::text = (storage.foldername(name))[1]
    );

CREATE POLICY "Users can view their own images"
    ON storage.objects FOR SELECT
    USING (
        bucket_id = 'canvas-images' AND
        auth.uid()::text = (storage.foldername(name))[1]
    );

CREATE POLICY "Users can delete their own images"
    ON storage.objects FOR DELETE
    USING (
        bucket_id = 'canvas-images' AND
        auth.uid()::text = (storage.foldername(name))[1]
    );