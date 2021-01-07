# Cloud Run for Anthos – Cloud Scheduler Events Java tutorial

This sample shows how to deploy a service in Cloud Run on Google Cloud and then consume events in that service from [Cloud Scheduler](https://cloud.google.com/scheduler).

For more details on how to work with this sample read the [Google Cloud Run Java Samples README](https://github.com/GoogleCloudPlatform/java-docs-samples/tree/master/run).

## Dependencies

* **Spring Boot**: Web server framework.
* **Junit + SpringBootTest**: [development] Test running framework.
* **MockMVC**: [development] Integration testing support framework.

## Setup

To complete this task, you must have an events broker and know in which namespace it's running. Learn how to [configure Events for KubeRun and create an events broker](https://cloud.google.com/eventarc/docs/kuberun/cluster-configuration).

If you have an events broker running, you can view the Kubernetes namespace by running:
```sh
kubectl get brokers -n ${NAMESPACE}
```

Configure environment variables:

```sh
export CLOUD_RUN_CONTAINER_NAME=cloud-run-container-java
export CLOUD_RUN_SERVICE_NAME=cloud-run-service-java
```

Enable APIs:
```sh
gcloud services enable cloudapis.googleapis.com 
gcloud services enable container.googleapis.com 
gcloud services enable containerregistry.googleapis.com
gcloud services enable cloudbuild.googleapis.com
gcloud services enable cloudscheduler.googleapis.com
```

## Quickstart

1. Clone this repo and change to java cloud scheduler folder:
```sh
cd java/cloud-scheduler
```

2. Build the container and upload it to Cloud Build:

```sh
gcloud builds submit \
   --tag gcr.io/$(gcloud config get-value project)/${CLOUD_RUN_CONTAINER_NAME}
```
It will ask for `API [cloudbuild.googleapis.com]` access if it’s not already enabled on the project.

3. Deploy the container image to Cloud Run for Anthos:
```sh
gcloud run deploy ${CLOUD_RUN_SERVICE_NAME}\
    --namespace=${NAMESPACE} \
    --image gcr.io/$(gcloud config get-value project)/${CLOUD_RUN_CONTAINER_NAME}
```
When you see the service URL, it has been successfully deployed.

4. Create an App Engine application:

Cloud Scheduler currently needs users to create an App Engine application. Pick an App Engine Location and create the app:

```sh
export APP_ENGINE_LOCATION=us-central
gcloud app create --region=${APP_ENGINE_LOCATION}
```

5. Create a trigger:
You can get more details on the parameters you'll need to construct a trigger for events from Google Cloud sources by running the following command:
```sh
gcloud beta events types describe google.cloud.scheduler.job.v1.executed
```
Pick a Cloud Scheduler location to create the scheduler:
```sh
export SCHEDULER_LOCATION=us-central1
```
(Important: Most of the time, APP_ENGINE_LOCATION and SCHEDULER_LOCATION are the same except two locations. Note that two locations, europe-west and us-central in App Engine, are called, respectively, europe-west1 and us-central1 in Cloud Scheduler.)

Create a Trigger that will create a job to be executed every minute in Google Cloud Scheduler and call the target service:
```sh
export TRIGGER_NAME=trigger-scheduler-java
gcloud beta events triggers create ${TRIGGER_NAME} \
  --namespace=${NAMESPACE} \
  --target-service=${CLOUD_RUN_SERVICE_NAME} \
  --type=google.cloud.scheduler.job.v1.executed \
  --parameters location=${SCHEDULER_LOCATION} \
  --parameters schedule="* * * * *" \
  --parameters data="trigger-scheduler-data"
```

6. Test the trigger:

List all triggers to confirm that trigger was successfully created:
```sh
gcloud beta events triggers list --namespace=${NAMESPACE}
```
Wait for up to 10 minutes for the trigger creation to be propagated and for it to begin filtering events. Once ready, it will filter events and send them to the service.

The output should be similar to the following:
```sh
TRIGGER                EVENT TYPE                              TARGET
trigger-scheduler-java google.cloud.scheduler.job.v1.executed  cloud-run-service-java
```

7. Verify
```sh
kubectl logs \
   --selector serving.knative.dev/service=${CLOUD_RUN_SERVICE_NAME} \
   -c user-container \
   -n ${NAMESPACE} \
   --tail=200
```
Cloud scheduler executes every minute and the KubeRun service logs the event's message. The output is similar to the following example:
```sh
[...]
2021-01-07 02:16:12.269  INFO 1 --- [           main] com.example.cloudrun.Application         : Starting Application using Java 11.0.9.1 on cloud-run-service-java-00001-paw-deployment-98c86bb67-hbl9l with PID 1 (/cloud-scheduler.jar started by root in /)
2021-01-07 02:16:12.277  INFO 1 --- [           main] com.example.cloudrun.Application         : No active profile set, falling back to default profiles: default
2021-01-07 02:16:17.311  INFO 1 --- [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat initialized with port(s): 8080 (http)
2021-01-07 02:16:17.332  INFO 1 --- [           main] o.apache.catalina.core.StandardService   : Starting service [Tomcat]
2021-01-07 02:16:17.333  INFO 1 --- [           main] org.apache.catalina.core.StandardEngine  : Starting Servlet engine: [Apache Tomcat/9.0.41]
2021-01-07 02:16:17.518  INFO 1 --- [           main] o.a.c.c.C.[Tomcat].[localhost].[/]       : Initializing Spring embedded WebApplicationContext
2021-01-07 02:16:17.519  INFO 1 --- [           main] w.s.c.ServletWebServerApplicationContext : Root WebApplicationContext: initialization completed in 4959 ms
2021-01-07 02:16:19.722  INFO 1 --- [           main] o.s.s.concurrent.ThreadPoolTaskExecutor  : Initializing ExecutorService 'applicationTaskExecutor'
2021-01-07 02:16:21.392  INFO 1 --- [           main] o.s.b.w.embedded.tomcat.TomcatWebServer  : Tomcat started on port(s): 8080 (http) with context path ''
2021-01-07 02:16:21.532  INFO 1 --- [           main] com.example.cloudrun.Application         : Started Application in 10.705 seconds (JVM running for 14.833)
2021-01-07 02:16:21.746  INFO 1 --- [nio-8080-exec-2] o.a.c.c.C.[Tomcat].[localhost].[/]       : Initializing Spring DispatcherServlet 'dispatcherServlet'
2021-01-07 02:16:21.747  INFO 1 --- [nio-8080-exec-2] o.s.web.servlet.DispatcherServlet        : Initializing Servlet 'dispatcherServlet'
2021-01-07 02:16:21.749  INFO 1 --- [nio-8080-exec-2] o.s.web.servlet.DispatcherServlet        : Completed initialization in 2 ms
Cloud Scheduler executed a job (id: 1896155548558759) at 2021-01-07T02:16:01.363Z
Cloud Scheduler executed a job (id: 1896171740783237) at 2021-01-07T02:18:00.115Z
Cloud Scheduler executed a job (id: 1896158226616373) at 2021-01-07T02:17:01.089Z
[...]
```

## Clean up

Delete the resources created in this tutorial to avoid recurring charges.
 
To delete the trigger, run:
```sh
gcloud beta events triggers delete ${TRIGGER_NAME}
```