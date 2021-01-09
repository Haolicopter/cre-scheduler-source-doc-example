// Copyright 2020 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

package main

import (
	"io/ioutil"
	"log"
	"os"
	"strings"
	"testing"
	"time"

	cloudevents "github.com/cloudevents/sdk-go/v2"
)

func TestReceive(t *testing.T) {
	tests := []struct {
		want string
	}{
		{want: "Cloud Scheduler executed a job (id: test-id)"},
	}
	for _, test := range tests {
		r, w, _ := os.Pipe()
		log.SetOutput(w)
		defer log.SetOutput(os.Stderr)

		originalFlags := log.Flags()
		defer log.SetFlags(originalFlags)
		log.SetFlags(log.Flags() &^ (log.Ldate | log.Ltime))

		// Create an Event.
		event := cloudevents.NewEvent()
		event.SetSource("test-uri")
		event.SetType("test-type")
		event.SetID("test-id")
		event.SetTime(time.Now())

		receive(event)

		w.Close()

		out, err := ioutil.ReadAll(r)
		if err != nil {
			t.Fatalf("ReadAll: %v", err)
		}
		if got := string(out); !strings.HasPrefix(got, test.want) {
			t.Errorf("Receive(): got %q, want %q", got, test.want)
		}
	}
}
