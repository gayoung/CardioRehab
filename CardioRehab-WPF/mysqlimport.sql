DROP TABLE IF EXISTS session_data;
DROP TABLE IF EXISTS patient_session;
DROP TABLE IF EXISTS doctor;
DROP TABLE IF EXISTS patient;
DROP TABLE IF EXISTS authentication;

--
-- Table structure for table AUTHENTICATION
--

CREATE TABLE IF NOT EXISTS authentication(
id int(10) NOT NULL AUTO_INCREMENT,
username varchar(20) NOT NULL,
password varchar(20) NOT NULL,
role varchar(20) NOT NULL,
PRIMARY KEY(id)
);

CREATE TABLE IF NOT EXISTS patient(
patient_id int(10) NOT NULL,
fname varchar(20) NOT NULL,
lname varchar(20) NOT NULL,
date_joined datetime,
date_birth datetime,
email varchar(100) NOT NULL,
wireless_ip varchar(20),
local_ip varchar(20),
doctor_id int(10) NOT NULL,
PRIMARY KEY(patient_id),
FOREIGN KEY(patient_id) REFERENCES authentication(id) ON DELETE CASCADE,
FOREIGN KEY(doctor_id) REFERENCES authentication(id) ON DELETE CASCADE
);
