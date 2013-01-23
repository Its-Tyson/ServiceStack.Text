﻿using NUnit.Framework;

namespace ServiceStack.Text.Tests.JsonTests
{
    [TestFixture]
    public class JsonObjectTests
    {
        [Test]
        public void Can_parse_empty_object()
        {
            Assert.That(JsonObject.Parse("{}"), Is.Empty);
        }

        [Test]
        public void Can_parse_empty_object_with_whitespaces()
        {
            Assert.That(JsonObject.Parse("{    }"), Is.Empty);
            Assert.That(JsonObject.Parse("{\n\n}"), Is.Empty);
            Assert.That(JsonObject.Parse("{\t\t}"), Is.Empty);
        }

        [Test]
        public void Can_parse_empty_object_with_mixed_whitespaces()
        {
            Assert.That(JsonObject.Parse("{ \n\t  \n\r}"), Is.Empty);
        }

        [Test]
        public void Can_parse_object_with_string_property()
        {
            var jsonObject = JsonObject.Parse(@"{""myStringProp"":""SomeStringValue""}");
            Assert.That(jsonObject.ContainsKey("myStringProp"));
            Assert.That(jsonObject["myStringProp"], Is.EqualTo("SomeStringValue"));
        }

        [Test]
        public void Can_parse_object_with_bool_property()
        {
            var jsonObject = JsonObject.Parse(@"{""myBoolProp"":true}");
            Assert.That(jsonObject.ContainsKey("myBoolProp"));
            Assert.That(jsonObject["myBoolProp"], Is.EqualTo(true));
        }

        [Test]
        public void Can_parse_object_with_int_property()
        {
            var jsonObject = JsonObject.Parse(@"{""myIntProp"":123}");
            Assert.That(jsonObject.ContainsKey("myIntProp"));
            Assert.That(jsonObject["myIntProp"], Is.EqualTo(123));
        }

        public class Jackalope
        {
            public string Name { get; set; }
            public Jackalope BabyJackalope { get; set; }
        }

        [Test]
        public void Can_serialise_json_object_deserialise_typed_object()
        {
            var jacks = new {
                Jack = new Jackalope { BabyJackalope = new Jackalope { Name = "in utero" } }
            };

            var jackString = JsonSerializer.SerializeToString(jacks.Jack);

            var jackJson = JsonObject.Parse(jackString);
            var jack = jackJson.Get<Jackalope>("BabyJackalope");

            Assert.That(jacks.Jack.BabyJackalope.Name, Is.EqualTo(jack.Name));

            var jackJsonString = jackJson.SerializeToString();
            Assert.That(jackString, Is.EqualTo(jackJsonString));

            var jackalope = JsonSerializer.DeserializeFromString<Jackalope>(jackJsonString);
            Assert.That(jackalope.BabyJackalope.Name, Is.EqualTo("in utero"));
        }
        
        readonly TextElementDto text = new TextElementDto {
            ElementId = "text_1",
            ElementType = "text",
            // Raw nesting - won't be escaped
            Content = new ElementContentDto { ElementId = "text_1", Content = "text goes here" },
            Action = new ElementActionDto { ElementId = "text_1", Action = "action goes here" }
        };

        readonly ImageElementDto image = new ImageElementDto {
            ElementId = "image_1",
            ElementType = "image",
            // String nesting - will be escaped
            Content = new ElementContentDto { ElementId = "image_1", Content = "image url goes here" }.ToJson(),
            Action = new ElementActionDto { ElementId = "image_1", Action = "action goes here" }.ToJson()
        };

        [Test]
        public void Can_Serialize_TypedContainerDto()
        {
            var container = new TypedContainerDto {
                Source = text,
                Destination = image
            };

            var json = container.ToJson();

            var fromJson = json.FromJson<TypedContainerDto>();

            Assert.That(container.Source.Action.ElementId, Is.EqualTo(fromJson.Source.Action.ElementId));

            var imgContent = container.Destination.Content.FromJson<ElementContentDto>();
            var fromContent = fromJson.Destination.Content.FromJson<ElementContentDto>();

            Assert.That(imgContent.ElementId, Is.EqualTo(fromContent.ElementId));
        }

        [Test]
        public void Can_DeSerialize_TypedContainerDto_with_JsonObject()
        {
            var container = new TypedContainerDto {
                Source = text,
                Destination = image
            };

            var json = container.ToJson();

            var fromText = JsonObject.Parse(json).Get<TextElementDto>("Source");

            Assert.That(container.Source.Action.ElementId, Is.EqualTo(fromText.Action.ElementId));
        }

        [Test]
        public void Can_DeSerialize_TypedContainerDto_into_JsonValueContainerDto()
        {
            var container = new TypedContainerDto {
                Source = text,
                Destination = image
            };

            var json = container.ToJson();

            var fromJson = json.FromJson<JsonValueContainerDto>();

            var fromText = fromJson.Source.As<TextElementDto>();
            var fromImage = fromJson.Destination.As<ImageElementDto>();

            Assert.That(container.Source.Action.ElementId, Is.EqualTo(fromText.Action.ElementId));
            Assert.That(container.Destination.ElementId, Is.EqualTo(fromImage.ElementId));

            Assert.That(container.Destination.Action, Is.EqualTo(fromImage.Action));
            Assert.That(container.Destination.Content, Is.EqualTo(fromImage.Content));
        }

        [Test]
        public void Can_Serialize_StringContainerDto()
        {
            var container = new StringContainerDto {
                Source = text.ToJson(),
                Destination = image.ToJson()
            };

            var json = container.ToJson();

            var fromJson = json.FromJson<StringContainerDto>();

            var src = container.Source.FromJson<TextElementDto>();
            var dst = container.Destination.FromJson<ImageElementDto>();

            var fromSrc = fromJson.Source.FromJson<TextElementDto>();
            var fromDst = fromJson.Destination.FromJson<ImageElementDto>();

            Assert.That(src.Action.ElementId, Is.EqualTo(fromSrc.Action.ElementId));
            Assert.That(dst.Action, Is.EqualTo(fromDst.Action));
        }
        
        public class TypedContainerDto
        {
            public TextElementDto Source { get; set; }
            public ImageElementDto Destination { get; set; }
        }

        // DTOs
        public class StringContainerDto // This is the request dto
        {
            public string Source { get; set; } // This will be some ElementDto
            public string Destination { get; set; } // This will be some ElementDto
        }

        // DTOs
        public class JsonValueContainerDto // This is the request dto
        {
            public JsonValue Source { get; set; } // This will be some ElementDto
            public JsonValue Destination { get; set; } // This will be some ElementDto
        }

        public class TextElementDto
        {
            public string ElementType { get; set; }
            public string ElementId { get; set; }

            public ElementContentDto Content { get; set; }
            public ElementActionDto Action { get; set; }
        }

        public class ImageElementDto
        {
            public string ElementType { get; set; }
            public string ElementId { get; set; }

            public string Content { get; set; }
            public string Action { get; set; }
        }

        public class ElementContentDto
        {
            public string ElementId { get; set; }
            public string Content { get; set; }
            // There can be more nested objects in here
        }

        public class ElementActionDto
        {
            public string ElementId { get; set; }
            public string Action { get; set; }
            // There can be more nested objects in here
        }
    }
}
