BEGIN TRANSACTION;
ALTER TABLE servermedia ADD LastPlayed INTEGER;
PRAGMA user_version = 2;
COMMIT;