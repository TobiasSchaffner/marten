﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Baseline;
using Marten.Events;
using Marten.Schema;
using Marten.Schema.Sequences;
using Marten.Services;
using Npgsql;

namespace Marten
{
    /// <summary>
    /// StoreOptions supplies all the necessary configuration
    /// necessary to customize and bootstrap a working
    /// DocumentStore
    /// </summary>
    public class StoreOptions
    {
        private ISerializer _serializer;
        private IConnectionFactory _factory;

        internal readonly IList<Type> PreBuiltStorage = new List<Type>(); 

        private readonly ConcurrentDictionary<Type, DocumentMapping> _documentMappings =
            new ConcurrentDictionary<Type, DocumentMapping>();

        public DocumentMapping MappingFor(Type documentType)
        {
            return _documentMappings.GetOrAdd(documentType, type => new DocumentMapping(type, this));
        }

        /// <summary>
        /// Add, remove, or reorder global session listeners
        /// </summary>
        public readonly IList<IDocumentSessionListener> Listeners = new List<IDocumentSessionListener>(); 

        public IEnumerable<DocumentMapping> AllDocumentMappings => _documentMappings.Values; 

        /// <summary>
        /// Upsert syntax options. Defaults to Postgresql <=9.4, but you can opt into
        /// the more efficient Postgresql 9.5 style upserts
        /// </summary>
        public PostgresUpsertType UpsertType { get; set; } = PostgresUpsertType.Legacy;


        /// <summary>
        /// Supply the connection string to the Postgresql database
        /// </summary>
        /// <param name="connectionString"></param>
        public void Connection(string connectionString)
        {
            _factory = new ConnectionFactory(connectionString);
        }

        /// <summary>
        /// Supply a source for the connection string to a Postgresql database
        /// </summary>
        /// <param name="connectionSource"></param>
        public void Connection(Func<string> connectionSource)
        {
            _factory = new ConnectionFactory(connectionSource);
        }

        /// <summary>
        /// Supply a mechanism for resolving an NpgsqlConnection object to
        /// the Postgresql database
        /// </summary>
        /// <param name="source"></param>
        public void Connection(Func<NpgsqlConnection> source)
        {
            _factory = new LambdaConnectionFactory(source);
        }

        /// <summary>
        /// Override the JSON serialization by ISerializer type
        /// </summary>
        /// <param name="serializer"></param>
        public void Serializer(ISerializer serializer)
        {
            _serializer = serializer;
        }

        /// <summary>
        /// Override the JSON serialization by an ISerializer of type "T"
        /// </summary>
        /// <typeparam name="T">The ISerializer type</typeparam>
        public void Serializer<T>() where T : ISerializer, new()
        {
            _serializer = new T();
        }

        /// <summary>
        /// Modify the document and event store database mappings for indexes and searching options
        /// </summary>
        public readonly MartenRegistry Schema = new MartenRegistry();

        /// <summary>
        /// Whether or Marten should attempt to create any missing database schema objects at runtime. This
        /// property is False by default for production usage, but should be set to True at development time for
        /// more efficient development. 
        /// </summary>
        public bool AutoCreateSchemaObjects = false;

        /// <summary>
        /// Global default parameters for Hilo sequences within the DocumentStore. Can be overridden per document
        /// type as well
        /// </summary>
        public HiloSettings HiloSequenceDefaults { get; } = new HiloSettings();

        /// <summary>
        /// Sets the batch size for updating or deleting documents in IDocumentSession.SaveChanges() / IUnitOfWork.ApplyChanges()
        /// </summary>
        public int UpdateBatchSize { get; set; } = 500;

        /// <summary>
        /// Force Marten to create document mappings for type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void RegisterDocumentType<T>()
        {
            RegisterDocumentType(typeof (T));
        }

        /// <summary>
        /// Force Marten to create a document mapping for the document type
        /// </summary>
        /// <param name="documentType"></param>
        public void RegisterDocumentType(Type documentType)
        {
            if (MappingFor(documentType) == null) throw new Exception("Unable to create document mapping for " + documentType);
        }

        /// <summary>
        /// Force Marten to create document mappings for all the given document types
        /// </summary>
        /// <param name="documentTypes"></param>
        public void RegisterDocumentTypes(IEnumerable<Type> documentTypes)
        {
            documentTypes.Each(RegisterDocumentType);
        }

        /// <summary>
        /// Configuration of event streams and projections
        /// </summary>
        public EventGraph Events { get; } = new EventGraph();

        internal ISerializer Serializer()
        {
            return _serializer ?? new JsonNetSerializer();
        }

        internal IConnectionFactory ConnectionFactory()
        {
            if (_factory == null) throw new InvalidOperationException("No database connection source is configured");

            return _factory;
        }

        /// <summary>
        /// Load pre-compiled document storage types from the assembly. You can
        /// use this mechanism to speed up your application startup time by
        /// avoiding the Roslyn just-in-time compilation
        /// </summary>
        /// <param name="assembly"></param>
        public void LoadPrecompiledStorageFrom(Assembly assembly)
        {
            PreBuiltStorage.AddRange(assembly.GetExportedTypes().Where(x => x.IsConcreteTypeOf<IDocumentStorage>()));
        }
    }
}