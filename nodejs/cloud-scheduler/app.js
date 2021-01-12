// Copyright 2020 Google, LLC.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

// [START event_handler]
const express = require('express');
const app = express();
const { v4: uuidv4 } = require('uuid');
const { HTTP, CloudEvent } = require("cloudevents");
const {toSchedulerJobData} = require('@google/events/cloud/scheduler/v1/SchedulerJobData');

app.use(express.json());
app.post('/', (req, res) => {
  if (!req.header('ce-id')) {
    return res
      .status(400)
      .send('Bad Request: missing required header: ce-id');
  }

  const receivedEvent = HTTP.toEvent({ headers: req.headers, body: req.body });
  const data = toSchedulerJobData(receivedEvent);
  console.log(`Cloud Scheduler executed a job (id: ${data.id}) at ${data.time}`);
  
  // reply with a cloudevent
  const replyEvent = new CloudEvent({
    id: uuidv4(),
    type: 'com.example.kuberun.events.received',
    source: 'https://localhost',
    specversion: '1.0',
  });
  replyEvent.data = {
    message: "Event received"
  }

  const message = HTTP.binary(replyEvent);
  return res.header(message.headers).status(200).send(message.body);
});

module.exports = app;
// [END event_handler]
