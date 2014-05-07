import django_tables2 as tables
from django_tables2.utils import A
from kinect.models import Patientstat, Patient

#This generates the view of how you would like your tables viewed

class PatientTable(tables.Table):
	Patients = tables.LinkColumn('Sessions', args=[A('pk')])
	class Meta:
		model = Patientstat
		exclude = ("id",)
		attrs = {"class": "paleblue"}
		
class O2Table(tables.Table):
	Patients = tables.LinkColumn('Sessions', args=[A('pk')])
	class Meta:
		model = Patientstat
		exclude = ("id","Hrmax", "Hrmin", "Hravg")
		attrs = {"class": "paleblue"}
		
class HeartTable(tables.Table):
	Patients = tables.LinkColumn('Sessions', args=[A('pk')])
	class Meta:
		model = Patientstat
		exclude = ("id","Oxymax", "Oxymin", "Oxyavg")
		attrs = {"class": "paleblue"}

class NameTable(tables.Table):
	Name = tables.LinkColumn('PatientStates', args=[A('pk')])
	class Meta:
		model = Patient
		exclude = ("id",)
		attrs = {"class": "paleblue"}