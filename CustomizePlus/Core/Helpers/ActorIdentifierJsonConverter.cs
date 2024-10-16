using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Penumbra.GameData.Actors;
using Newtonsoft.Json.Linq;

namespace CustomizePlus.Core.Helpers;

internal sealed class ActorIdentifierJsonConverter : JsonConverter<ActorIdentifier>
{
    public override ActorIdentifier ReadJson(JsonReader reader, Type objectType, ActorIdentifier existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        if (Penumbra.GameData.Actors.ActorIdentifierExtensions.Manager == null)
            throw new Exception("Penumbra.GameData.Actors.ActorIdentifierExtensions.Manager is not ready");

        return Penumbra.GameData.Actors.ActorIdentifierExtensions.Manager.FromJson(obj);
    }

    public override void WriteJson(JsonWriter writer, ActorIdentifier value, Newtonsoft.Json.JsonSerializer serializer)
    {
        value.ToJson().WriteTo(writer);
    }
}