/*
 * Copyright 2020 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package com.example.kuberun.events;

// [START scheduler_handler]
import java.net.URI;
import java.util.UUID;

import org.springframework.http.HttpHeaders;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RestController;

import io.cloudevents.CloudEvent;
import io.cloudevents.core.message.MessageReader;
import io.cloudevents.http.HttpMessageFactory;
import io.cloudevents.rw.CloudEventRWException;
import io.cloudevents.spring.http.CloudEventHttpUtils;

@RestController
public class EventController {

  @PostMapping("/")
  public ResponseEntity<String> receiveMessage(@RequestHeader HttpHeaders headers, @RequestBody String body) {
    try {
      MessageReader reader = createMessageReader(headers, body);
      CloudEvent event = reader.toEvent();
      System.out.println("Cloud Scheduler executed a job (id: " + event.getId() + ") at " + event.getTime());
    } catch (IllegalStateException | CloudEventRWException e) {
      return ResponseEntity.badRequest().body("Malformed event");
    }

    CloudEvent attributes = CloudEventHttpUtils.fromHttp(headers)
        .withId(UUID.randomUUID().toString()) //
        .withSource(URI.create("https://localhost")) //
        .withType("com.example.kuberun.events.received") //
        .build();

    HttpHeaders replyHeaders = CloudEventHttpUtils.toHttp(attributes);
    return ResponseEntity.ok().headers(replyHeaders).body("Event received");
  }

  private static MessageReader createMessageReader(HttpHeaders headers, String body) {
    return HttpMessageFactory.createReader(headers.toSingleValueMap(), body.getBytes());
  }
}
// [END scheduler_handler]
