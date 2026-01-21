using System.Diagnostics.CodeAnalysis;
using Content.Shared._Lavaland.Megafauna.Selectors;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Content.Shared._Lavaland.Megafauna;

[TypeSerializer]
public sealed class MegafaunaSelectorTypeSerializer :
    ITypeReader<MegafaunaSelector, MappingDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        // Reserve edit - If the type is explicitly specified via !type: tag, validate that specific type
        if (node.Tag?.StartsWith("!type:") == true)
        {
            var typeString = node.Tag.Substring(6);
            var type = dependencies.Resolve<IReflectionManager>().GetType(typeString);
            if (type != null && typeof(MegafaunaSelector).IsAssignableFrom(type))
                return serializationManager.ValidateNode(type, node, context);
        }

        if (node.Has(ProtoIdMegafaunaSelector.IdDataFieldTag))
            return serializationManager.ValidateNode<ProtoIdMegafaunaSelector>(node, context);

        return new ErrorNode(node, "Custom validation not supported! Please specify the type manually!");
    }

    public MegafaunaSelector Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MegafaunaSelector>? instanceProvider = null)
    {
        var type = typeof(MegafaunaSelector);
        if (node.Has(ProtoIdMegafaunaSelector.IdDataFieldTag))
            type = typeof(ProtoIdMegafaunaSelector);

        return (MegafaunaSelector) serializationManager.Read(type, node, context)!;
    }
}
