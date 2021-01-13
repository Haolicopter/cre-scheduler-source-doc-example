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

using System;
using System.Net.Mime;
using CloudNative.CloudEvents;
using Google.Events;
using Google.Events.Protobuf.Cloud.Scheduler.V1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

                var cloudEvent = await context.Request.ReadCloudEventAsync();
                if (string.IsNullOrEmpty(cloudEvent.Id))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("Bad Request: expected header ce-id");
                    return;
                }
                logger.LogInformation($"Cloud Scheduler executed a job (id: {cloudEvent.Id}) at {cloudEvent.Time}");
                var schedulerJobData = CloudEventConverters.ConvertCloudEventData<SchedulerJobData>(cloudEvent);
                logger.LogInformation($"Scheduler data: {schedulerJobData.CustomData.ToBase64()}");

                // reply with a cloudevent
                var replyEvent = new CloudEvent(
                    CloudEventsSpecVersion.V1_0, 
                    "com.example.kuberun.events.received",
                    new Uri("https://localhost"), 
                    Guid.NewGuid().ToString(),
                    DateTime.Now)
                {
                    DataContentType = new ContentType(MediaTypeNames.Application.Json),
                    Data = JsonConvert.SerializeObject("Event received")
                };

                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.Headers.Add("Ce-Id", replyEvent.Id);
                context.Response.Headers.Add("Ce-Specversion", "1.0");
                context.Response.Headers.Add("Ce-Type", replyEvent.Type);
                context.Response.Headers.Add("Ce-Source", replyEvent.Source.ToString());
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(replyEvent.Data.ToString());
            });
        });
    }
}
// [END event_handler]
