CREATE TABLE IF NOT EXISTS mst_trip_type (
    id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL,
    name VARCHAR(150) NOT NULL,
    description TEXT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    created_by INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by INTEGER NULL,
    updated_at TIMESTAMP WITHOUT TIME ZONE NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_mst_trip_type_code
    ON mst_trip_type (code);

CREATE UNIQUE INDEX IF NOT EXISTS ux_mst_trip_type_name
    ON mst_trip_type (name);

INSERT INTO mst_trip_type (code, name, description, created_by)
VALUES
    ('ONE_WAY', 'One Way', 'Single destination trip', 0),
    ('ROUND_TRIP', 'Round Trip', 'Pickup and return trip', 0),
    ('LOCAL', 'Local', 'Local duty trip within city limits', 0)
ON CONFLICT (code) DO NOTHING;
