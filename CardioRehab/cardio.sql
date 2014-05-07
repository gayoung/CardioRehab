/* This is a sample populate SQL */

DROP TABLE IF EXISTS authentication;

CREATE TABLE authentication(
id INTEGER PRIMARY KEY,
username CHAR(20) NOT NULL,
password CHAR(20) NOT NULL,
role CHAR(10) NOT NULL
);

INSERT INTO authentication (username, password, role) VALUES ('admin', 'admin', 'Admin');