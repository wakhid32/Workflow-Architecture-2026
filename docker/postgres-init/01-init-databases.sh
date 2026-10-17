#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE DATABASE workflow_engine_db;
    CREATE DATABASE security_identity_db;
    CREATE DATABASE keycloak_db;
    GRANT ALL PRIVILEGES ON DATABASE workflow_engine_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE security_identity_db TO $POSTGRES_USER;
    GRANT ALL PRIVILEGES ON DATABASE keycloak_db TO $POSTGRES_USER;
EOSQL
