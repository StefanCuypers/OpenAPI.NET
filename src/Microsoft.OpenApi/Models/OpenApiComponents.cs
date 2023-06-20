﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Json.More;
using Json.Schema;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Writers;
//using SharpYaml.Serialization;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;


namespace Microsoft.OpenApi.Models
{
    /// <summary>
    /// Components Object.
    /// </summary>
    public class OpenApiComponents : IOpenApiSerializable, IOpenApiExtensible
    {
        /// <summary>
        /// An object to hold reusable <see cref="JsonSchema"/> Objects.
        /// </summary>
        public IDictionary<string, JsonSchema> Schemas31 { get; set; } = new Dictionary<string, JsonSchema>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiResponse"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiResponse> Responses { get; set; } = new Dictionary<string, OpenApiResponse>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiParameter"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiParameter> Parameters { get; set; } =
            new Dictionary<string, OpenApiParameter>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiExample"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiExample> Examples { get; set; } = new Dictionary<string, OpenApiExample>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiRequestBody"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiRequestBody> RequestBodies { get; set; } =
            new Dictionary<string, OpenApiRequestBody>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiHeader"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiHeader> Headers { get; set; } = new Dictionary<string, OpenApiHeader>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiSecurityScheme"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiSecurityScheme> SecuritySchemes { get; set; } =
            new Dictionary<string, OpenApiSecurityScheme>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiLink"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiLink> Links { get; set; } = new Dictionary<string, OpenApiLink>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiCallback"/> Objects.
        /// </summary>
        public IDictionary<string, OpenApiCallback> Callbacks { get; set; } = new Dictionary<string, OpenApiCallback>();

        /// <summary>
        /// An object to hold reusable <see cref="OpenApiPathItem"/> Object.
        /// </summary>
        public IDictionary<string, OpenApiPathItem> PathItems { get; set; } = new Dictionary<string, OpenApiPathItem>();

        /// <summary>
        /// This object MAY be extended with Specification Extensions.
        /// </summary>
        public IDictionary<string, IOpenApiExtension> Extensions { get; set; } = new Dictionary<string, IOpenApiExtension>();

        /// <summary>
        /// The indentation string to prepand to each line for each indentation level.
        /// </summary>
        protected const string IndentationString = "  ";

        /// <summary>
        /// Parameter-less constructor
        /// </summary>
        public OpenApiComponents() { }

        /// <summary>
        /// Initializes a copy of an <see cref="OpenApiComponents"/> object
        /// </summary>
        public OpenApiComponents(OpenApiComponents components)
        {
            Schemas31 = components?.Schemas31 != null ? new Dictionary<string, JsonSchema>(components.Schemas31) : null;
            Responses = components?.Responses != null ? new Dictionary<string, OpenApiResponse>(components.Responses) : null;
            Parameters = components?.Parameters != null ? new Dictionary<string, OpenApiParameter>(components.Parameters) : null;
            Examples = components?.Examples != null ? new Dictionary<string, OpenApiExample>(components.Examples) : null;
            RequestBodies = components?.RequestBodies != null ? new Dictionary<string, OpenApiRequestBody>(components.RequestBodies) : null;
            Headers = components?.Headers != null ? new Dictionary<string, OpenApiHeader>(components.Headers) : null;
            SecuritySchemes = components?.SecuritySchemes != null ? new Dictionary<string, OpenApiSecurityScheme>(components.SecuritySchemes) : null;
            Links = components?.Links != null ? new Dictionary<string, OpenApiLink>(components.Links) : null;
            Callbacks = components?.Callbacks != null ? new Dictionary<string, OpenApiCallback>(components.Callbacks) : null;
            PathItems = components?.PathItems != null ? new Dictionary<string, OpenApiPathItem>(components.PathItems) : null;
            Extensions = components?.Extensions != null ? new Dictionary<string, IOpenApiExtension>(components.Extensions) : null;
        }

        /// <summary>
        /// Serialize <see cref="OpenApiComponents"/> to Open API v3.1.
        /// </summary>
        /// <param name="writer"></param>
        public void SerializeAsV31(IOpenApiWriter writer)
        {
            writer = writer ?? throw Error.ArgumentNull(nameof(writer));

            // If references have been inlined we don't need the to render the components section
            // however if they have cycles, then we will need a component rendered
            if (writer.GetSettings().InlineLocalReferences)
            {
                RenderComponents(writer);
                return;
            }

            writer.WriteStartObject();

            // pathItems - only present in v3.1
            writer.WriteOptionalMap(
            OpenApiConstants.PathItems,
            PathItems,
            (w, key, component) =>
            {
                if (component.Reference != null &&
                    component.Reference.Type == ReferenceType.Schema &&
                    component.Reference.Id == key)
                {
                    component.SerializeAsV31WithoutReference(w);
                }
                else
                {
                    component.SerializeAsV31(w);
                }
            });

            SerializeInternal(writer, OpenApiSpecVersion.OpenApi3_1, (writer, element) => element.SerializeAsV31(writer),
               (writer, referenceElement) => referenceElement.SerializeAsV31WithoutReference(writer));
        }

        /// <summary>
        /// Serialize <see cref="OpenApiComponents"/> to v3.0
        /// </summary>
        /// <param name="writer"></param>
        public void SerializeAsV3(IOpenApiWriter writer)
        {
            writer = writer ?? throw Error.ArgumentNull(nameof(writer));

            // If references have been inlined we don't need the to render the components section
            // however if they have cycles, then we will need a component rendered
            if (writer.GetSettings().InlineLocalReferences)
            {
                RenderComponents(writer);
                return;
            }

            writer.WriteStartObject();
            SerializeInternal(writer, OpenApiSpecVersion.OpenApi3_0, (writer, element) => element.SerializeAsV3(writer),
                (writer, referenceElement) => referenceElement.SerializeAsV3WithoutReference(writer));
        }

        /// <summary>
        /// Serialize <see cref="OpenApiComponents"/>.
        /// </summary>
        private void SerializeInternal(IOpenApiWriter writer, OpenApiSpecVersion version,
            Action<IOpenApiWriter, IOpenApiSerializable> callback, Action<IOpenApiWriter, IOpenApiReferenceable> action)
        {
            // Serialize each referenceable object as full object without reference if the reference in the object points to itself.
            // If the reference exists but points to other objects, the object is serialized to just that reference.

            // schemas
            if (Schemas31 != null && Schemas31.Any())
            {
                if (writer is OpenApiYamlWriter)
                {
                    var document = Schemas31.ToJsonDocument();
                    var yamlNode = ConvertJsonToYaml(document.RootElement);
                    var serializer = new SerializerBuilder()
                                        .Build();

                    var yamlSchema = serializer.Serialize(yamlNode);

                    writer.WritePropertyName(OpenApiConstants.Schemas);
                    writer.WriteRaw(yamlSchema);
                }
                else
                {
                    writer.WritePropertyName(OpenApiConstants.Schemas);
                    writer.WriteRaw(JsonSerializer.Serialize(Schemas31));
                }
            }

            // responses
            writer.WriteOptionalMap(
                OpenApiConstants.Responses,
                Responses,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.Response &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // parameters
            writer.WriteOptionalMap(
                OpenApiConstants.Parameters,
                Parameters,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.Parameter &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // examples
            writer.WriteOptionalMap(
                OpenApiConstants.Examples,
                Examples,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.Example &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(writer, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // requestBodies
            writer.WriteOptionalMap(
                OpenApiConstants.RequestBodies,
                RequestBodies,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.RequestBody &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))

                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // headers
            writer.WriteOptionalMap(
                OpenApiConstants.Headers,
                Headers,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.Header &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // securitySchemes
            writer.WriteOptionalMap(
                OpenApiConstants.SecuritySchemes,
                SecuritySchemes,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.SecurityScheme &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // links
            writer.WriteOptionalMap(
                OpenApiConstants.Links,
                Links,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.Link &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // callbacks
            writer.WriteOptionalMap(
                OpenApiConstants.Callbacks,
                Callbacks,
                (w, key, component) =>
                {
                    if (component.Reference != null &&
                        component.Reference.Type == ReferenceType.Callback &&
                        string.Equals(component.Reference.Id, key, StringComparison.OrdinalIgnoreCase))
                    {
                        action(w, component);
                    }
                    else
                    {
                        callback(w, component);
                    }
                });

            // extensions
            writer.WriteExtensions(Extensions, version);
            writer.WriteEndObject();
        }

        private void RenderComponents(IOpenApiWriter writer)
        {
            var loops = writer.GetSettings().LoopDetector.Loops;
            writer.WriteStartObject();
            if (loops.TryGetValue(typeof(JsonSchema), out List<object> schemas))
            {

                writer.WriteRaw(JsonSerializer.Serialize(schemas));
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Serialize <see cref="OpenApiComponents"/> to Open Api v2.0.
        /// </summary>
        public void SerializeAsV2(IOpenApiWriter writer)
        {
            // Components object does not exist in V2.
        }

        private static YamlNode ConvertJsonToYaml(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var yamlObject = new YamlMappingNode();
                    foreach (var property in element.EnumerateObject())
                    {
                        yamlObject.Add(property.Name, ConvertJsonToYaml(property.Value));
                    }
                    return yamlObject;

                case JsonValueKind.Array:
                    var yamlArray = new YamlSequenceNode();
                    foreach (var item in element.EnumerateArray())
                    {
                        yamlArray.Add(ConvertJsonToYaml(item));
                    }
                    return yamlArray;

                case JsonValueKind.String:
                    return new YamlScalarNode(element.GetString());

                case JsonValueKind.Number:
                    return new YamlScalarNode(element.GetRawText());

                case JsonValueKind.True:
                    return new YamlScalarNode("true");

                case JsonValueKind.False:
                    return new YamlScalarNode("false");

                case JsonValueKind.Null:
                    return new YamlScalarNode("null");

                default:
                    throw new NotSupportedException($"Unsupported JSON value kind: {element.ValueKind}");
            }
        }
    }
}
