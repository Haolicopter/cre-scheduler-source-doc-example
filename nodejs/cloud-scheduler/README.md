# Cloud Run for Anthos – Cloud Scheduler Events nodejs tutorial


This sample shows how to deploy a service in Cloud Run on Google Cloud and then consume events in that service from [Cloud Scheduler](https://cloud.google.com/scheduler).

For more details on how to work with this sample read the [Google Cloud Run Node.js Samples README](https://github.com/GoogleCloudPlatform/nodejs-docs-samples/tree/master/run).

## Dependencies

* **express**: Web server framework.
* **mocha**: [development] Test running framework.
* **supertest**: [development] HTTP assertion test client.

To complete this task, you must have an events broker and know in which namespace it's running. Learn how to [configure Events for KubeRun and create an events broker](https://cloud.google.com/eventarc/docs/kuberun/cluster-configuration).

If you have an events broker running, you can view the Kubernetes namespace by running:
```sh
kubectl get brokers -n ${NAMESPACE}
```

Configure environment variables:

```sh
export CLOUD_RUN_CONTAINER_NAME=cloud-run-container-nodejs
export CLOUD_RUN_SERVICE_NAME=cloud-run-service-nodejs
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

1. Clone this repo and change to nodejs cloud scheduler folder:
```sh
cd nodejs/cloud-scheduler
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
export TRIGGER_NAME=trigger-scheduler-nodejs
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
TRIGGER                  EVENT TYPE                              TARGET
trigger-scheduler-nodejs google.cloud.scheduler.job.v1.executed  cloud-run-service-nodejs
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
> cloud-run-events-scheduler@1.0.0 start /usr/src/app
> node index.js

nodejs-events-scheduler listening on port 8080
Cloud Scheduler executed a job (id: 1896015583883664) at 2021-01-07T00:19:00.6Z
Cloud Scheduler executed a job (id: 1896127647107936) at 2021-01-07T01:41:00.211Z
Cloud Scheduler executed a job (id: 1896084574486870) at 2021-01-07T01:10:00.983Z
Cloud Scheduler executed a job (id: 1896128771586258) at 2021-01-07T01:48:00.418Z
Cloud Scheduler executed a job (id: 1896128771586258) at 2021-01-07T01:48:00.418Z
Cloud Scheduler executed a job (id: 1896130493267003) at 2021-01-07T01:45:00.317Z
Cloud Scheduler executed a job (id: 1896058578231641) at 2021-01-07T00:50:00.415Z
Cloud Scheduler executed a job (id: 1896128771586258) at 2021-01-07T01:48:00.418Z
Cloud Scheduler executed a job (id: 1896133274768454) at 2021-01-07T01:47:00.37Z
Cloud Scheduler executed a job (id: 1896128771586258) at 2021-01-07T01:48:00.418Z
Cloud Scheduler executed a job (id: 1896040976309653) at 2021-01-07T00:39:00.127Z
[...]
```

## Clean up

Delete the resources created in this tutorial to avoid recurring charges.
 
To delete the trigger, run:
```sh
gcloud beta events triggers delete ${TRIGGER_NAME}
```