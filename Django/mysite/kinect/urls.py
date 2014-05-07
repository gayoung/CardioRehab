from django.conf.urls import patterns, url

from kinect import views

# URL generator

urlpatterns = patterns('',
	url(r'^$', 'django.contrib.auth.views.login', {'template_name': 'login.html'}),
	url(r'^main/', views.main),
	url(r'^patients/(\d+)/', views.patients, name ='PatientStates'),
	url(r'^sessions/(\d+)/', views.session, name ='Sessions'),
	url(r'^patientstats/', views.patientstats),
	url(r'^o2stats/', views.o2stats),
	url(r'^heartstats/', views.heartstats),
	url(r'^addpatients/', views.addpatients),
	url(r'^data/', views.data),
	url(r'^logout/', 'django.contrib.auth.views.logout', {'template_name': 'login.html'}),
)