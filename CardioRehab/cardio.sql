/* This is a sample populate SQL */

DROP TABLE IF EXISTS doctor_patient_relation;
DROP TABLE IF EXISTS doctor;
DROP TABLE IF EXISTS patient;
DROP TABLE IF EXISTS authentication;

/* foreign key support is not enabled by default in SQLite*/
PRAGMA foreign_keys = ON;

CREATE TABLE authentication(
id INTEGER PRIMARY KEY,
username CHAR(20) NOT NULL,
password CHAR(20) NOT NULL,
role CHAR(10) NOT NULL
);

CREATE TABLE patient(
patient_id INTEGER NOT NULL,
fname CHAR(20) NOT NULL,
lname CHAR(20) NOT NULL,
date_joined TEXT, /* SQLITE stores date as text/real/integer types */
date_birth TEXT,
email CHAR(30) NOT NULL,
local_ip CHAR(20),
wireless_ip CHAR(20),
doctor_id INTEGER NOT NULL,
PRIMARY KEY(patient_id),
FOREIGN KEY(patient_id) REFERENCES authentication(id),
FOREIGN KEY(doctor_id) REFERENCES authentication(id)
);

CREATE TABLE doctor(
doctor_id INTEGER NOT NULL,
fname CHAR(20),
lname CHAR(20),
doc_code CHAR(30) NOT NULL, /* physician's code?*/
date_joined TEXT, /* SQLITE stores date as text/real/integer types */
email CHAR(30) NOT NULL,
local_ip CHAR(20),
PRIMARY KEY(doctor_id),
FOREIGN KEY(doctor_id) REFERENCES authentication(id)
);

INSERT INTO authentication (username, password, role) VALUES ('admin', 'admin', 'Admin');
INSERT INTO authentication (username, password, role) VALUES ('patient1', 'test', 'Patient');
INSERT INTO authentication (username, password, role) VALUES ('doctor1', 'test', 'Doctor');

INSERT INTO doctor (doctor_id, fname, lname, doc_code, date_joined, email, local_ip) VALUES(
(SELECT id FROM authentication WHERE username = 'doctor1' AND password = 'test'),
'Matt', 'Smith', '123456', CURRENT_TIMESTAMP, 'doc@example.com', ''
);

INSERT INTO patient VALUES(
(SELECT id FROM authentication WHERE username = 'patient1' AND password = 'test'),
'Amelia', 'Pond', CURRENT_TIMESTAMP, '1988-01-01', 'test@example.com', '', '',
(SELECT id FROM authentication WHERE username = 'doctor1' AND password = 'test')
);