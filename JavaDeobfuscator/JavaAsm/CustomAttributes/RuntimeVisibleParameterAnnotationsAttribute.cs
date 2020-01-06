﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BinaryEncoding;
using JavaDeobfuscator.JavaAsm.CustomAttributes.Annotation;
using JavaDeobfuscator.JavaAsm.IO;
using JavaDeobfuscator.JavaAsm.IO.ConstantPoolEntries;

namespace JavaDeobfuscator.JavaAsm.CustomAttributes
{
    internal class RuntimeVisibleParameterAnnotationsAttribute : CustomAttribute
    {        
        public List<ParameterAnnotations> Parameters { get; set; } = new List<ParameterAnnotations>();

        public override byte[] Save(ClassWriterState writerState, AttributeScope scope)
        {
            using var attributeDataStream = new MemoryStream();

            if (Parameters.Count > byte.MaxValue)
                throw new ArgumentOutOfRangeException($"Number of parameters is too big: {Parameters.Count} > {byte.MaxValue}");
            attributeDataStream.WriteByte((byte)Parameters.Count);
            foreach (var parameter in Parameters)
            {
                if (parameter.Annotations.Count > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(
                        $"Number of annotations is too big: {parameter.Annotations.Count} > {ushort.MaxValue}");
                Binary.BigEndian.Write(attributeDataStream, (ushort) parameter.Annotations.Count);
                foreach (var annotation in parameter.Annotations)
                    annotation.Write(attributeDataStream, writerState);
            }

            return attributeDataStream.ToArray();
        }
    }

    internal class RuntimeVisibleParameterAnnotationsAttributeFactory : ICustomAttributeFactory<RuntimeVisibleParameterAnnotationsAttribute>
    {
        public RuntimeVisibleParameterAnnotationsAttribute Parse(Stream attributeDataStream, uint attributeDataLength, ClassReaderState readerState, AttributeScope scope)
        {
            var attribute = new RuntimeVisibleParameterAnnotationsAttribute();

            var parametersCount = (byte)attributeDataStream.ReadByte();
            attribute.Parameters.Capacity = parametersCount;
            for (var i = 0; i < parametersCount; i++)
            {
                var parameter = new ParameterAnnotations();
                var annotationsCount = Binary.BigEndian.ReadUInt16(attributeDataStream);
                parameter.Annotations.Capacity = annotationsCount;
                for (var j = 0; j < annotationsCount; j++)
                    parameter.Annotations.Add(AnnotationNode.Parse(attributeDataStream, readerState));
                attribute.Parameters.Add(parameter);
            }

            return attribute;
        }
    }
}
