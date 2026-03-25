CREATE TABLE IF NOT EXISTS category (
    id VARCHAR(36) NOT NULL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    type ENUM('product', 'service') NOT NULL
);
