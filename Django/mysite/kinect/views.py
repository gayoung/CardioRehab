import os
from django.shortcuts import render
from django.shortcuts import render_to_response
from django.template import RequestContext, loader
from django_tables2   import RequestConfig
from django.http import HttpResponse, Http404
from django.contrib.auth.decorators import login_required
from django.views.decorators.csrf import csrf_exempt
from django.contrib.auth import logout
from django.utils import timezone
from django.core.exceptions import ObjectDoesNotExist

from kinect.models import Patientstat, Patient, Session
from kinect.tables import PatientTable, NameTable, O2Table, HeartTable

# This is to generate back-end for all the web pages
# This will do the posting of data from the Kinect App

def login(request):
	context = RequestContext(request)
	print django.contrib.auth
	return render_to_response('login.html', context)

def main(request):
	context = RequestContext(request)
	table = NameTable(Patient.objects.all())
	RequestConfig(request).configure(table)
	if request.user.is_authenticated():
		return render(request, 'main.html', {'table': table})
	else:
		return render_to_response('login.html', context)
		
def addpatients(request):
	context = {}
	if request.method == 'POST' :
		context['name'] = request.POST['name']
		context['bday'] = request.POST['bday']
		try:
			Patient.objects.get(Name = context['name'])
			html ="<html><body>Customer already exists</body> <INPUT TYPE=\"button\" VALUE=\"Back\" onClick=\"history.back()\"></html>"
			return HttpResponse(html)
		except ObjectDoesNotExist:
			Patient.objects.create(Name = context['name'], Date = context['bday'])
	if request.user.is_authenticated():
		return render_to_response('addpatients.html', RequestContext(request, context))
	else:
		return render_to_response('login.html', RequestContext(request, context))

def patients(request, Patient_id):
	context = RequestContext(request)
	table = PatientTable(Patientstat.objects.filter(Patients=Patient_id))
	RequestConfig(request).configure(table)
	if request.user.is_authenticated():
		return render(request, 'patients.html', {'table': table})
	else:
		return render_to_response('login.html', context)	
		
def patientstats(request):
	context = RequestContext(request)
	table = PatientTable(Patientstat.objects.all())
	RequestConfig(request).configure(table)
	if request.user.is_authenticated():
		return render(request, 'allstats.html', {'table': table})
	else:
		return render_to_response('login.html', context)
		
def o2stats(request):
	context = RequestContext(request)
	table =O2Table(Patientstat.objects.all())
	RequestConfig(request).configure(table)
	if request.user.is_authenticated():
		return render(request, 'o2stats.html', {'table': table})
	else:
		return render_to_response('login.html', context)	

def heartstats(request):
	context = RequestContext(request)
	table =HeartTable(Patientstat.objects.all())
	RequestConfig(request).configure(table)
	if request.user.is_authenticated():
		return render(request, 'heartstats.html', {'table': table})
	else:
		return render_to_response('login.html', context)		

	
@csrf_exempt
def data(request):
	if request.method == 'POST' :
		fullname = request.POST['name']
		oxymax = request.POST['OXmax']
		oxymin = request.POST['OXmin']
		oxyavg = request.POST['OXavg']
		hrmax = request.POST['hrmax']
		hrmin = request.POST['hrmin']
		hravg = request.POST['hravg']
		bp = request.POST['bp']
		try:
			context = Patient.objects.get(Name = fullname)
		except ObjectDoesNotExist:
			return HttpResponse("Fail, Customer Does not exist")
		Patientstat.objects.create(Patients = context, Date = timezone.now(), DTime = timezone.now(), Oxymax = oxymax, Oxymin = oxymin, Oxyavg = oxyavg, Hrmax = hrmax, Hrmin = hrmin, Hravg = hravg, BP = bp)
		return HttpResponse(context)
	else:
		return HttpResponse("fail")

@csrf_exempt		
def session(request, Session_id):
	id = Patientstat.objects.get(id=Session_id)
	try:
		Session.objects.get(id=Session_id)
		Text = Session.objects.get(id=Session_id).text
	except ObjectDoesNotExist:
		Session.objects.create(id = Session_id, Patients = id, text ='')
	if request.method == 'POST' :
		Tex= request.POST['text']
		Session.objects.filter(id=Session_id).update(text = Tex)
	if request.user.is_authenticated():
		return render(request, 'session.html', {'id': id, 'text': Text})
	else:
		return render_to_response('login.html', id)	
	
	
	
	
	
	


