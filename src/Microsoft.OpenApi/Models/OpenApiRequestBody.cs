﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Writers;

namespace Microsoft.OpenApi.Models
{
    /// <summary>
    /// Request Body Object
    /// </summary>
    public class OpenApiRequestBody : IOpenApiReferenceable, IOpenApiExtensible, IOpenApiRequestBody
    {
        /// <inheritdoc />
        public string Description { get; set; }

        /// <inheritdoc />
        public bool Required { get; set; }

        /// <inheritdoc />
        public IDictionary<string, OpenApiMediaType> Content { get; set; } = new Dictionary<string, OpenApiMediaType>();

        /// <inheritdoc />
        public IDictionary<string, IOpenApiExtension> Extensions { get; set; } = new Dictionary<string, IOpenApiExtension>();

        /// <summary>
        /// Parameter-less constructor
        /// </summary>
        public OpenApiRequestBody() { }

        /// <summary>
        /// Initializes a copy instance of an <see cref="IOpenApiRequestBody"/> object
        /// </summary>
        public OpenApiRequestBody(IOpenApiRequestBody requestBody)
        {
            Utils.CheckArgumentNull(requestBody);
            Description = requestBody?.Description ?? Description;
            Required = requestBody?.Required ?? Required;
            Content = requestBody?.Content != null ? new Dictionary<string, OpenApiMediaType>(requestBody.Content) : null;
            Extensions = requestBody?.Extensions != null ? new Dictionary<string, IOpenApiExtension>(requestBody.Extensions) : null;
        }

        /// <summary>
        /// Serialize <see cref="OpenApiRequestBody"/> to Open Api v3.1
        /// </summary>
        public void SerializeAsV31(IOpenApiWriter writer)
        {
            SerializeInternal(writer, OpenApiSpecVersion.OpenApi3_1, (writer, element) => element.SerializeAsV31(writer));
        }

        /// <summary>
        /// Serialize <see cref="OpenApiRequestBody"/> to Open Api v3.0
        /// </summary>
        public void SerializeAsV3(IOpenApiWriter writer)
        {
            SerializeInternal(writer, OpenApiSpecVersion.OpenApi3_0, (writer, element) => element.SerializeAsV3(writer));
        }
        
        internal void SerializeInternal(IOpenApiWriter writer, OpenApiSpecVersion version,
            Action<IOpenApiWriter, IOpenApiSerializable> callback)
        {
            Utils.CheckArgumentNull(writer);

            writer.WriteStartObject();

            // description
            writer.WriteProperty(OpenApiConstants.Description, Description);

            // content
            writer.WriteRequiredMap(OpenApiConstants.Content, Content, callback);

            // required
            writer.WriteProperty(OpenApiConstants.Required, Required, false);

            // extensions
            writer.WriteExtensions(Extensions, version);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Serialize <see cref="OpenApiRequestBody"/> to Open Api v2.0
        /// </summary>
        public void SerializeAsV2(IOpenApiWriter writer)
        {
            // RequestBody object does not exist in V2.
        }

        /// <inheritdoc/>
        public IOpenApiParameter ConvertToBodyParameter(IOpenApiWriter writer)
        {
            var bodyParameter = new OpenApiBodyParameter
            {
                Description = Description,
                // V2 spec actually allows the body to have custom name.
                // To allow round-tripping we use an extension to hold the name
                Name = "body",
                Schema = Content.Values.FirstOrDefault()?.Schema ?? new OpenApiSchema(),
                Examples = Content.Values.FirstOrDefault()?.Examples,
                Required = Required,
                Extensions = Extensions.ToDictionary(static k => k.Key, static v => v.Value)  // Clone extensions so we can remove the x-bodyName extensions from the output V2 model.
            };
            if (bodyParameter.Extensions.ContainsKey(OpenApiConstants.BodyName))
            {
                var bodyName = bodyParameter.Extensions[OpenApiConstants.BodyName] as OpenApiAny;
                bodyParameter.Name = string.IsNullOrEmpty(bodyName?.Node.ToString()) ? "body" : bodyName?.Node.ToString();
                bodyParameter.Extensions.Remove(OpenApiConstants.BodyName);
            }
            return bodyParameter;
        }

        /// <inheritdoc/>
        public IEnumerable<IOpenApiParameter> ConvertToFormDataParameters(IOpenApiWriter writer)
        {
            if (Content == null || !Content.Any())
                yield break;

            foreach (var property in Content.First().Value.Schema.Properties)
            {
                var paramSchema = property.Value;
                if ((paramSchema.Type & JsonSchemaType.String) == JsonSchemaType.String
                    && ("binary".Equals(paramSchema.Format, StringComparison.OrdinalIgnoreCase)
                    || "base64".Equals(paramSchema.Format, StringComparison.OrdinalIgnoreCase)))
                {
                    paramSchema.Type = "file".ToJsonSchemaType();
                    paramSchema.Format = null;
                }
                yield return new OpenApiFormDataParameter()
                {
                    Description = property.Value.Description,
                    Name = property.Key,
                    Schema = property.Value,
                    Examples = Content.Values.FirstOrDefault()?.Examples,
                    Required = Content.First().Value.Schema.Required?.Contains(property.Key) ?? false
                };
            }
        }
    }
}
