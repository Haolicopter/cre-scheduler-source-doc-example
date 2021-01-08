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

// [START event_handler]

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CloudNative.CloudEvents;
using Google.Events;
using Google.Events.Protobuf.Cloud.Scheduler.V1;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        logger.LogInformation("Service is starting...");

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapPost("/", async context =>
            {
                logger.LogInformation("Handling HTTP POST");

                var ceId = context.Request.Headers["ce-id"];
                var ceTime = context.Request.Headers["ce-time"];
                logger.LogInformation($"ce-id: {ceId}");

                var cloudEvent = await context.Request.ReadCloudEventAsync();
                var data = CloudEventConverters.ConvertCloudEventData<SchedulerJobData>(cloudEvent);
                logger.LogInformation($"Scheduler data: {data.CustomData.ToBase64()}");

                if (string.IsNullOrEmpty(ceId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("Bad Request: expected header ce-id");
                    return;
                }

                logger.LogInformation($"Cloud Scheduler executed a job (id: {ceId}) at {ceTime}");
                await context.Response.WriteAsync("");
            });
        });
    }
}
// [END event_handler]
