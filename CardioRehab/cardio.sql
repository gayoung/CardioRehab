/* This is a sample populate SQL */

DROP TABLE IF EXISTS session_data;
DROP TABLE IF EXISTS patient_session;
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

CREATE TABLE patient_session(
id INTEGER PRIMARY KEY,
patient_id INTEGER NOT NULL,
name CHAR(50), /* if nurse..etc are monitoring instead */
label CHAR(20) NOT NULL, /* label of patient in software (patient1-6) */
date_start TEXT NOT NULL,
date_end TEXT NOT NULL,
hrmax REAL NOT NULL,
hrmin REAL NOT NULL,
hravg REAL NOT NULL,
oxmax REAL NOT NULL,
oxmin REAL NOT NULL,
oxavg REAL NOT NULL,
sysmax REAL NOT NULL,
sysmin REAL NOT NULL,
sysavg REAL NOT NULL,
diamax REAL NOT NULL,
diamin REAL NOT NULL,
diaavg REAL NOT NULL,
FOREIGN KEY(patient_id) REFERENCES authentication(id)
);

CREATE TABLE session_data(
id INTEGER PRIMARY KEY,
session_id INTEGER,
heart_rate INTEGER NOT NULL,
oxygen INTEGER NOT NULL,
systolic INTEGER NOT NULL,
diastolic INTEGER NOT NULL,
note TEXT,
recorded_date TEXT NOT NULL,
FOREIGN KEY(session_id) REFERENCES patient_session(id)
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