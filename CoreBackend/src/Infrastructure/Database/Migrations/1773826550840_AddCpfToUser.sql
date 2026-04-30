ALTER TABLE user ADD COLUMN cpf TEXT NOT NULL DEFAULT '';
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_cpf_unique ON user(cpf) WHERE cpf != '' AND deletedAt IS NULL;
