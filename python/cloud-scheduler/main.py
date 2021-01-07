# Copyright 2020 Google, LLC.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#    http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# [START event_receiver]
import os

from flask import Flask, request

app = Flask(__name__)
# [END event_receiver]

# [START event_handler]
@app.route('/', methods=['POST'])
def index():
    # Get the event id and time from the CloudEvent header
    id = request.headers.get('ce-id')
    time = request.headers.get('ce-time')

    print(f"Cloud Scheduler executed a job (id: {id}) at {time}")
    return (f"Cloud Scheduler executed a job (id: {id}) at {time}", 200)
# [END event_handler]

# [START event_receiver]
if __name__ == "__main__":
    app.run(debug=True, host='0.0.0.0', port=int(os.environ.get('PORT', 8080)))
# [END event_receiver]
