from django.contrib import admin
from kinect.models import Patient, Patientstat, Session

admin.site.register(Patient)
admin.site.register(Patientstat)
admin.site.register(Session)
