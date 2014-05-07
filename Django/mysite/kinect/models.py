import datetime
from django.db import models
from django.utils import timezone

# This is the SQL tables that will be created

class Patient(models.Model):
	Name = models.CharField(max_length = 45)
	Date = models.DateField('Date of Birth')
	def __str__(self):
		return self.Name
	
		
class Patientstat(models.Model):
	Patients = models.ForeignKey(Patient)
	Date = models.DateTimeField('Excercise Date')
	Oxymax = models.CharField(max_length=5)
	Oxymin = models.CharField(max_length=5)
	Oxyavg = models.CharField(max_length=5)
	Hrmax = models.CharField(max_length=5)
	Hrmin = models.CharField(max_length=5)
	Hravg = models.CharField(max_length=5)
	BP = models.CharField(max_length=5)
	def __str__(self):
		return self.Patients.Name + ", " + str(self.Date)
class Session(models.Model):
	Patients = models.ForeignKey(Patientstat)
	text = models.TextField(null=True)
	def __str__(self):
		return str(self.Patients)
	
	

