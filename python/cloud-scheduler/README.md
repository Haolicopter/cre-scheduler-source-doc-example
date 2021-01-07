# Cloud Run for Anthos – Cloud Scheduler Events Python tutorial

This sample shows how to deploy a service in Cloud Run on Google Cloud and then consume events in that service from [Cloud Scheduler](https://cloud.google.com/scheduler).

## Setup

To complete this task, you must have an events broker and know in which namespace it's running. Learn how to [configure Events for KubeRun and create an events broker](https://cloud.google.com/eventarc/docs/kuberun/cluster-configuration).

If you have an events broker running, you can view the Kubernetes namespace by running:
```sh
kubectl get brokers -n ${NAMESPACE}
```

Configure environment variables:

```sh
export CLOUD_RUN_CONTAINER_NAME=cloud-run-container-python
export CLOUD_RUN_SERVICE_NAME=cloud-run-service-python
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

1. Clone this repo and change to python cloud scheduler folder:
```sh
cd python/cloud-scheduler
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
export TRIGGER_NAME=trigger-scheduler-python
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
trigger-scheduler-python google.cloud.scheduler.job.v1.executed  cloud-run-service-python
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
[2021-01-06 19:09:11 +0000] [1] [INFO] Starting gunicorn 20.0.4
[2021-01-06 19:09:11 +0000] [1] [INFO] Listening at: http://0.0.0.0:8080 (1)
[2021-01-06 19:09:11 +0000] [1] [INFO] Using worker: threads
[2021-01-06 19:09:11 +0000] [9] [INFO] Booting worker with pid: 9
Cloud Scheduler executed a job (id: 1895784854321278) at 2021-01-06T21:23:00.906Z
Cloud Scheduler executed a job (id: 1895777403474729) at 2021-01-06T21:22:00.87Z
Cloud Scheduler executed a job (id: 1895724001509680) at 2021-01-06T20:42:00.53Z
Cloud Scheduler executed a job (id: 1895776713301837) at 2021-01-06T21:20:00.817Z
Cloud Scheduler executed a job (id: 1895779212269203) at 2021-01-06T21:25:00.955Z
Cloud Scheduler executed a job (id: 1895600549918871) at 2021-01-06T19:16:00.768Z
Cloud Scheduler executed a job (id: 1895703814844765) at 2021-01-06T20:23:00.87Z
Cloud Scheduler executed a job (id: 1895779212269203) at 2021-01-06T21:25:00.955Z
Cloud Scheduler executed a job (id: 1895614731087771) at 2021-01-06T19:26:00.319Z
Cloud Scheduler executed a job (id: 1895779212269203) at 2021-01-06T21:25:00.955Z
Cloud Scheduler executed a job (id: 1895776618815324) at 2021-01-06T21:21:00.85Z
Cloud Scheduler executed a job (id: 1895782925754810) at 2021-01-06T21:24:00.929Z
[...]
```

## Clean up

Delete the resources created in this tutorial to avoid recurring charges.
 
To delete the trigger, run:
```sh
gcloud beta events triggers delete ${TRIGGER_NAME}
```