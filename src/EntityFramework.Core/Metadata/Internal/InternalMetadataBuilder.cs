// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Infrastructure;
using Microsoft.Data.Entity.Internal;

namespace Microsoft.Data.Entity.Metadata.Internal
{
    public abstract class InternalMetadataBuilder
    {
        private readonly LazyRef<Dictionary<string, ConfigurationSource>> _annotationSources =
            new LazyRef<Dictionary<string, ConfigurationSource>>(() => new Dictionary<string, ConfigurationSource>());

        protected InternalMetadataBuilder([NotNull] Annotatable metadata)
        {
            Metadata = metadata;
        }

        public virtual bool Annotation(
            [NotNull] string annotation, [CanBeNull] object value, ConfigurationSource configurationSource)
        {
            var existingValue = Metadata[annotation];
            if (existingValue != null)
            {
                ConfigurationSource existingConfigurationSource;
                if (!_annotationSources.Value.TryGetValue(annotation, out existingConfigurationSource))
                {
                    existingConfigurationSource = ConfigurationSource.Explicit;
                }

                if ((value == null || existingValue != value)
                    && !configurationSource.Overrides(existingConfigurationSource))
                {
                    return false;
                }

                configurationSource = configurationSource.Max(existingConfigurationSource);
            }

            if (value != null)
            {
                _annotationSources.Value[annotation] = configurationSource;
                Metadata[annotation] = value;
            }
            else
            {
                _annotationSources.Value.Remove(annotation);
                Metadata.RemoveAnnotation(new Annotation(annotation, "_"));
            }

            return true;
        }

        public virtual Annotatable Metadata { get; }

        public abstract InternalModelBuilder ModelBuilder { get; }
    }
}
