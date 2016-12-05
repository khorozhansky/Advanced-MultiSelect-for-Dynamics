namespace TuneMultiSelect
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using Entities;
  using Exception;
  using Microsoft.Crm.Sdk.Messages;
  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Client;
  using Microsoft.Xrm.Sdk.Query;
  using Utils;

  /// <summary>
  /// Extended Base class for Plug-ins.
  /// </summary>
  /// <typeparam name="TEntity">
  /// The type of the entity.
  /// </typeparam>
  /// <remarks>
  /// Just inherit from PluginBase<Entity/> in case the plugin is to handle different entities in a single plugin 
  /// </remarks>
  public abstract class PluginBase<TEntity> : IPlugin
    where TEntity : Entity, new()
  {
    /// <summary>
    /// The registered entity logical name (the name is cached)
    /// </summary>
    public readonly string RegisteredEntityLogicalName;

    /// <summary>
    /// The secure config
    /// </summary>
    private readonly string secureConfig;

    /// <summary>
    /// The unsecure config
    /// </summary>
    private readonly string unsecureConfig;

    /// <summary>
    /// The Backing Field for <seealso cref="RegisteredPluginSteps"/>
    /// </summary>
    private IList<PluginStepBase> registeredPluginSteps;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginBase{TEntity}"/> class.
    /// </summary>
    /// <param name="childClassName">
    /// Name of the child class.
    /// </param>
    /// <param name="unsecureConfig">
    /// The unsecure config.
    /// </param>
    /// <param name="secureConfig">
    /// The secure config.
    /// </param>
    public PluginBase(Type childClassName, string unsecureConfig = null, string secureConfig = null)
    {
      var entityType = typeof(TEntity);
      this.RegisteredEntityLogicalName =
        entityType == typeof(Entity) ? string.Empty : typeof(TEntity).Name.ToLower();

      this.ChildClassName = childClassName.ToString();
      this.unsecureConfig = unsecureConfig;
      this.secureConfig = secureConfig;
    }

    /// <summary>
    /// Gets the registered plugin steps.
    /// </summary>
    /// <value>
    /// The registered plugin steps.
    /// </value>
    protected IList<PluginStepBase> RegisteredPluginSteps
    {
      get
      {
        return this.registeredPluginSteps ?? (this.registeredPluginSteps = new List<PluginStepBase>());
      }
    }

    /// <summary>
    /// Gets the name of the child class.
    /// </summary>
    /// <value>The name of the child class.</value>
    protected string ChildClassName { get; private set; }

    /// <summary>
    /// Executes the plug-in.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider.
    /// </param>
    /// <remarks>
    /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
    /// The plug-in's Execute method should be written to be stateless as the constructor 
    /// is not called for every invocation of the plug-in. Also, multiple system threads 
    /// could execute the plug-in at the same time. All per invocation state information 
    /// is stored in the context. This means that you should not use global variables in plug-ins.
    /// </remarks>
    public void Execute(IServiceProvider serviceProvider)
    {
      if (serviceProvider == null)
      {
        throw new ArgumentNullException("serviceProvider");
      }

      // Construct the Local plug-in context.
      using (var localContext = new PluginContext(serviceProvider, this.RegisteredEntityLogicalName, this.unsecureConfig, this.secureConfig))
      {
        var stage = localContext.ExecContext.Stage;
        var mode = localContext.ExecContext.Mode;
        var messageName = localContext.ExecContext.MessageName;
        var primaryEntityName = localContext.ExecContext.PrimaryEntityName;

        localContext.Trace(
          string.Format(
            CultureInfo.InvariantCulture,
            "{0} is firing for Entity: {1}, Message: {2}, Stage: {3}, Mode: {4}",
            this.ChildClassName,
            primaryEntityName,
            messageName,
            stage,
            mode));

        try
        {
          Func<PluginStepBase, bool> stepSearchCondition =
            step =>
            (step.Stage == null || (int)step.Stage == stage) && (step.Mode == null || (int)step.Mode == mode)
            && (step.MessageName == null || step.MessageName.ToString() == messageName)
            && (string.IsNullOrWhiteSpace(this.RegisteredEntityLogicalName) || primaryEntityName == "none" || this.RegisteredEntityLogicalName == primaryEntityName);

          var entityAction =
            this.RegisteredPluginSteps.Where(stepSearchCondition).Select(step => step.Handler).FirstOrDefault();

          if (entityAction == null)
          {
            localContext.Trace("No appropriate registered action found.");
          }
          else
          {
            localContext.Trace("An appropriate registered action has been found. Executing the action...");
            entityAction(localContext);
          }
        }
        catch (PluginIgnoredException ignoredException)
        {
          ExceptionHelper.BuildInvalidPluginExecutionException(
            ignoredException,
            this.GetType(),
            localContext.TracingService);
        }
        catch (InvalidPluginExecutionException)
        {
          throw;
        }
        catch (System.Exception e)
        {
          throw ExceptionHelper.BuildInvalidPluginExecutionException(
            e,
            this.GetType(),
            localContext.TracingService);
        }
      }
    }

    /// <summary>
    /// Defines the local plugin context class.
    /// </summary>
    public class PluginContext : IPluginContext, IDisposable
    {
      /// <summary>
      /// The secure config
      /// </summary>
      internal readonly string SecureConfig;

      /// <summary>
      /// The unsecure config
      /// </summary>
      internal readonly string UnsecureConfig;
      
      #region PluginParameterKeyNames

      /// <summary> The default pre image name </summary>
      private const string DefaultPreImageName = "PreImage";

      /// <summary> The default post image name </summary>
      private const string DefaultPostImageName = "PostImage";

      /// <summary>
      /// The target key.
      /// </summary>
      private const string TargetKey = "Target";

      /// <summary>
      /// The business entity key.
      /// </summary>
      private const string BusinessEntityKey = "BusinessEntity";

      /// <summary>
      /// The entity moniker key.
      /// </summary>
      private const string EntityMonikerKey = "EntityMoniker";

      /// <summary>
      /// The state key.
      /// </summary>
      private const string StateKey = "State";

      /// <summary>
      /// The status key.
      /// </summary>
      private const string StatusKey = "Status";

      /// <summary>
      /// The parameter xml key.
      /// </summary>
      private const string ParameterXmlKey = "ParameterXml";

      /// <summary>
      /// The related entities key.
      /// </summary>
      private const string RelatedEntitiesKey = "RelatedEntities";

      /// <summary>
      /// The relationship key.
      /// </summary>
      private const string RelationshipKey = "Relationship";

      /// <summary>
      /// The subordinate id key.
      /// </summary>
      private const string SubordinateIdKey = "SubordinateId";

      /// <summary>
      /// The column set key.
      /// </summary>
      private const string ColumnSetKey = "ColumnSet";

      /// <summary>
      /// The query key.
      /// </summary>
      private const string QueryKey = "Query";

      /// <summary>
      /// The business entity collection key.
      /// </summary>
      private const string BusinessEntityCollectionKey = "BusinessEntityCollection";

      /// <summary>
      /// The assignee key.
      /// </summary>
      private const string AssigneeKey = "Assignee";

      /// <summary>
      /// The principal key.
      /// </summary>
      private const string PrincipalKey = "Principal";

      /// <summary>
      /// The access rights key.
      /// </summary>
      private const string AccessRightsKey = "AccessRights";

      /// <summary>
      /// The update content key.
      /// </summary>
      private const string UpdateContentKey = "UpdateContent";

      #endregion

      /// <summary>
      /// The service provider
      /// </summary>
      private readonly IServiceProvider serviceProvider;

      /// <summary>
      /// Determines if the object is disposed
      /// </summary>
      private bool disposed;

      #region BackingFields

      /// <summary>
      /// Backing Field for <see cref="OrganizationServiceFactory" />
      /// </summary>
      private IOrganizationServiceFactory organizationServiceFactory;

      /// <summary>
      /// Backing Field for <see cref="ExecContext" />
      /// </summary>
      private IPluginExecutionContext executionContext;

      /// <summary>
      /// Backing Field for <see cref="TracingService" />
      /// </summary>
      private ITracingService tracingService;

      /// <summary>
      /// Backing Field for <see cref="Service" />
      /// </summary>
      private IOrganizationService service;

      /// <summary>
      /// Backing Field for <see cref="ServiceAsSystemUser" />
      /// </summary>
      private IOrganizationService serviceAsSystemUser;

      /// <summary>
      /// Backing Field for <see cref="PrimaryEntityRef" />
      /// </summary>
      private EntityReference primaryEntityRef;

      /// <summary>
      /// Backing Field for <see cref="OrgCtx" />
      /// </summary>
      private CrmContext orgCtx;

      /// <summary>
      /// Backing Field for <see cref="OrgCtxAsSystemUser" />
      /// </summary>
      private CrmContext orgCtxAsSystemUser;

      /// <summary>
      /// Backing Field for <see cref="InputTarget"/>
      /// </summary>
      private TEntity inputTarget;

      /// <summary>
      /// Backing Field for  <see cref="RegisteredEntityLogicalName" />
      /// </summary>
      private string registeredEntityLogicalName;

      /// <summary>
      /// Backing Field for  <see cref="InputTargetAsEntity"/>
      /// </summary>
      private Entity inputTargetAsEntity;

      /// <summary>
      /// Backing Field for <see cref="TargetExt"/>
      /// </summary>
      private EntityExtended<TEntity> targetExt;

      /// <summary>
      /// Backing Field for <see cref="PostImageExt"/>
      /// </summary>
      private EntityExtended<TEntity> postImageExt;

      /// <summary>
      /// Backing Field for  <see cref="OutputBusinessEntityEntity"/>
      /// </summary>
      private Entity outputBusinessEntityEntity;

      /// <summary>
      /// Backing Field for  <see cref="OutputBusinessEntity"/>
      /// </summary>
      private TEntity outputBusinessEntity;

      /// <summary>
      /// Backing Field for  <see cref="Relationship"/>
      /// </summary>
      private Relationship relationship;

      /// <summary>
      /// Backing Field for  <see cref="RelatedEntities"/>
      /// </summary>
      private EntityReferenceCollection relatedEntities;

      /// <summary>
      /// Backing Field for  <see cref="InputTargetEntityReference"/>
      /// </summary>      
      private EntityReference inputTargetAsEntityReference;

      /// <summary>
      /// Backing Field for <see cref="Principal"/>
      /// </summary>
      private EntityReference principal;

      /// <summary>
      /// Backing Field for <see cref="InputColumnSet"/>
      /// </summary>
      private ColumnSet inputColumnSet;

      /// <summary>
      /// Backing Field for <see cref="State"/>
      /// </summary>
      private OptionSetValue state;

      /// <summary>
      /// Backing Field for <see cref="Status"/>
      /// </summary>
      private OptionSetValue status;

      /// <summary>
      /// Backing Field for <see cref="ParameterXml"/>
      /// </summary>
      private string parameterXml;

      /// <summary>
      /// Backing Field for  <see cref="SubordinateId"/>
      /// </summary>
      private Guid? subordinateReference;

      /// <summary>
      /// Backing Field for  <see cref="EntityMoniker"/>
      /// </summary>
      private EntityReference entityMoniker;

      /// <summary>
      /// Backing Field for  <see cref="PreImage"/>
      /// </summary>
      private TEntity preImage;

      /// <summary>
      /// Backing Field for  <see cref="PostImage"/>
      /// </summary>
      private TEntity postImage;

      /// <summary>
      /// Backing field for <see cref="QueryExpr"/>
      /// </summary>
      private QueryExpression query;

      /// <summary>
      /// Backing field for <see cref="Assignee"/>
      /// </summary>
      private EntityReference assignee;

      /// <summary>
      /// Backing Field for  <see cref="UpdateContentEntity"/>
      /// </summary>
      private Entity updateContentEntity;

      /// <summary>
      /// Backing Field for  <see cref="UpdateContent"/>
      /// </summary>
      private TEntity updateContent;

      /// <summary>
      /// Backing Field for  <see cref="AccessRights"/>
      /// </summary>
      private AccessRights? accessRights;

      #endregion

      /// <summary>
      /// Initializes a new instance of the <see cref="PluginContext" /> class.
      /// </summary>
      /// <param name="serviceProvider">The service provider.</param>
      /// <param name="registeredEntityLogicalName">Name of the registered entity logical.</param>
      /// <param name="unsecureConfig">The unsecure config.</param>
      /// <param name="secureConfig">The secure config.</param>
      /// <exception cref="ArgumentNullException">serviceProvider</exception>
      /// <exception cref="System.ArgumentNullException">serviceProvider</exception>
      public PluginContext(IServiceProvider serviceProvider, string registeredEntityLogicalName, string unsecureConfig = null, string secureConfig = null)
      {
        if (serviceProvider == null)
        {
          throw new ArgumentNullException("serviceProvider");
        }

        this.registeredEntityLogicalName = registeredEntityLogicalName;
        this.serviceProvider = serviceProvider;
        this.UnsecureConfig = unsecureConfig;
        this.SecureConfig = secureConfig;
      }

      /// <summary>
      /// Prevents a default instance of the <see cref="PluginContext"/> class from being created. 
      /// Initializes a new instance of the <see cref="PluginContext"/> class.
      /// </summary>
      // ReSharper disable once UnusedMember.Local
      private PluginContext()
      {
      }

      /// <summary>
      /// Gets the plugin execution context.
      /// </summary>
      /// <value>The plugin execution context.</value>
      public IPluginExecutionContext ExecContext
      {
        get
        {
          return this.executionContext ??
            (this.executionContext = (IPluginExecutionContext)this.serviceProvider.GetService(typeof(IPluginExecutionContext))); 
        }
      }

      /// <summary>
      /// Gets the tracing service.
      /// </summary>
      /// <value>The tracing service.</value>
      public ITracingService TracingService
      {
        get
        {
          return this.tracingService ??
            (this.tracingService = (ITracingService)this.serviceProvider.GetService(typeof(ITracingService)));
        }
      }

      /// <summary>
      /// Gets the organization service for the "current user".
      /// </summary>
      /// <value>The organization service.</value>
      public IOrganizationService Service
      {
        get
        {
            if (this.service != null)
            {
                return this.service;
            }

            var currentUserId = this.ExecContext.UserId;
            this.service = this.OrganizationServiceFactory.CreateOrganizationService(currentUserId);

            return this.service;
        }
      }

      /// <summary>
      /// Gets the organization service for a system user.
      /// </summary>
      /// <value>
      /// The organization service for system user.
      /// </value>
      public IOrganizationService ServiceAsSystemUser
      {
        get
        {
          return this.serviceAsSystemUser ?? 
            (this.serviceAsSystemUser = this.OrganizationServiceFactory.CreateOrganizationService(null));
        }
      }

      /// <summary>
      /// Gets the organization context for the "current user".
      /// </summary>
      /// <value>The organization context.</value>
      public CrmContext OrgCtx
      {
        get
        {
          return this.orgCtx ??
            (this.orgCtx = new CrmContext(this.Service) { MergeOption = MergeOption.NoTracking });
        }
      }

      /// <summary>
      /// Gets the organization context for a system user.
      /// </summary>
      /// <value>The organization context as system user.</value>
      public CrmContext OrgCtxAsSystemUser
      {
        get
        {
          return this.orgCtxAsSystemUser ??
            (this.orgCtxAsSystemUser = new CrmContext(this.ServiceAsSystemUser) { MergeOption = MergeOption.NoTracking });
        }
      }

      /// <summary>
      /// Gets the primary entity identifier.
      /// </summary>
      /// <value>
      /// The primary entity identifier.
      /// </value>
      public Guid PrimaryEntityId
      {
        get
        {
          return this.ExecContext.PrimaryEntityId;
        }
      }

      /// <summary>
      /// Gets the name of the primary entity.
      /// </summary>
      /// <value>
      /// The name of the primary entity.
      /// </value>
      public string PrimaryEntityName
      {
        get
        {
          return this.ExecContext.PrimaryEntityName;
        }
      }

      /// <summary>
      /// Gets the primary entity reference.
      /// </summary>
      /// <value>
      /// The primary entity reference.
      /// </value>
      public EntityReference PrimaryEntityRef
      {
        get
        {
          return this.primaryEntityRef ?? 
            (this.primaryEntityRef = new EntityReference(this.PrimaryEntityName, this.PrimaryEntityId));
        }
      }

      /// <summary>
      /// Gets the name of the entity for the handler registered.
      /// </summary>
      /// <value>
      /// The name of the base entity logical.
      /// </value>
      public string RegisteredEntityLogicalName
      {
        get
        {
          return this.registeredEntityLogicalName;
        }
      }

      /// <summary>
      /// Gets or sets the input target.
      /// </summary>
      /// <value>The input target.</value>
      public TEntity InputTarget
      {
        get
        {
          if (this.inputTarget == null)
          {
            if (this.InputTargetAsEntity != null)
            {
              this.inputTarget = this.InputTargetAsEntity.ToEntity<TEntity>();
            }
          }

          return this.inputTarget;
        }

        set
        {
          this.inputTargetAsEntity = value;
        }
      }

      /// <summary>
      /// Gets or sets the input target entity.
      /// </summary>
      /// <value>The input target entity.</value>
      public Entity InputTargetAsEntity
      {
        get
        {
          return this.inputTargetAsEntity ?? (this.inputTargetAsEntity = this.GetInputParameter<Entity>(TargetKey));
        }

        set
        {
          this.inputTargetAsEntity = value;
          this.SetInputParameter(TargetKey, value);
        }
      }

      /// <summary>
      /// Gets the target extended.
      /// </summary>
      /// <value>
      /// The post image extended as <see cref="EntityExtended{TEntity}"/>.
      /// </value>
      public EntityExtended<TEntity> TargetExt
      {
        get
        {
          return this.targetExt ?? (this.targetExt = new EntityExtended<TEntity>(this.InputTarget, this.PreImage));
        }
      }

      /// <summary>
      /// Gets the post image extended.
      /// </summary>
      /// <value>
      /// The post image extended as <see cref="EntityExtended{TEntity}"/>.
      /// </value>
      public EntityExtended<TEntity> PostImageExt
      {
        get
        {
          return this.postImageExt ?? (this.postImageExt = new EntityExtended<TEntity>(this.PostImage, this.PreImage));
        }
      }

      /// <summary>
      /// Gets the input column set.
      /// </summary>
      /// <value>
      /// The input column set.
      /// </value>
      public ColumnSet InputColumnSet
      {
        get
        {
          return this.inputColumnSet ?? (this.inputColumnSet = this.GetInputParameter<ColumnSet>(ColumnSetKey));
        }
      }

      /// <summary>
      /// Gets or sets the output business entity.
      /// </summary>
      /// <value>The output business entity.</value>
      public TEntity OutputBusinessEntity
      {
        get
        {
          if (this.outputBusinessEntity == null)
          {
            if (this.OutputBusinessEntityEntity != null)
            {
              this.outputBusinessEntity = this.OutputBusinessEntityEntity.ToEntity<TEntity>();
            }
          }

          return this.outputBusinessEntity;
        }

        set
        {
          this.OutputBusinessEntityEntity = value;
        }
      }

      /// <summary>
      /// Gets or sets the output business entity entity (as Entity).
      /// </summary>
      /// <value>The output business entity entity.</value>
      public Entity OutputBusinessEntityEntity
      {
        get
        {
          return this.outputBusinessEntityEntity
                 ?? (this.outputBusinessEntityEntity = this.GetOutputParameter<Entity>(BusinessEntityKey));
        }

        set
        {
          this.outputBusinessEntityEntity = value;
          this.SetOutputParameter(BusinessEntityKey, value);
        }
      }

      /// <summary>
      /// Gets the output business entity collection.
      /// </summary>
      /// <value>
      /// The output business entity collection.
      /// </value>
      public EntityCollection OutputBusinessEntityCollection
      {
        get
        {
          return this.GetOutputParameter<EntityCollection>(BusinessEntityCollectionKey);
        }
      }

      /// <summary>
      /// Gets the input target entity reference.
      /// </summary>
      /// <value>The input target entity reference.</value>
      public EntityReference InputTargetEntityReference
      {
        get
        {
          return this.inputTargetAsEntityReference
                 ?? (this.inputTargetAsEntityReference = this.GetInputParameter<EntityReference>(TargetKey));
        }
      }

      /// <summary>
      /// Gets the principal.
      /// </summary>
      /// <value>The principal.</value>
      public EntityReference Principal
      {
        get
        {
          return this.principal
                 ?? (this.principal = this.GetInputParameter<EntityReference>(PrincipalKey));
        }
      }

      /// <summary>
      /// Gets or sets the access rights.
      /// </summary>
      /// <value>The access rights.</value>
      public AccessRights? AccessRights
      {
        get
        {
          if (this.accessRights != null)
          {
            return this.accessRights;
          }

          var value = this.GetInputParameter(AccessRightsKey);
          if (value == null)
          {
            this.accessRights = null;
          }
          else
          {
            this.accessRights = (AccessRights)value;
          }

          return this.accessRights;
        }

        set
        {
          this.accessRights = value;
          this.SetInputParameter(AccessRightsKey, value);
        }
      }

      /// <summary>
      /// Gets the state of the input.
      /// </summary>
      /// <value>The state of the input.</value>
      public OptionSetValue State
      {
        get
        {
          return this.state ?? (this.state = this.GetInputParameter<OptionSetValue>(StateKey));
        }
      }

      /// <summary>
      /// Gets the input status.
      /// </summary>
      /// <value>The input status.</value>
      public OptionSetValue Status
      {
        get
        {
          return this.status ?? (this.status = this.GetInputParameter<OptionSetValue>(StatusKey));
        }
      }

      /// <summary>
      /// Gets the relationship.
      /// </summary>
      /// <value>The relationship.</value>
      public Relationship Relationship
      {
        get
        {
          return this.relationship ?? (this.relationship = this.GetInputParameter<Relationship>(RelationshipKey));
        }
      }

      /// <summary>
      /// Gets the related entities.
      /// </summary>
      /// <value>The related entities.</value>
      public EntityReferenceCollection RelatedEntities
      {
        get
        {
          return this.relatedEntities
                 ?? (this.relatedEntities = this.GetInputParameter<EntityReferenceCollection>(RelatedEntitiesKey));
        }
      }

      /// <summary>
      /// Gets the ParameterXML parameter.
      /// </summary>
      /// <value>The parameter XML.</value>
      public string ParameterXml
      {
        get
        {
          return this.parameterXml ?? (this.parameterXml = this.GetInputParameter<string>(ParameterXmlKey));
        }
      }

      /// <summary>
      /// Gets the subordinate reference.
      /// </summary>
      /// <value>
      /// The subordinate reference.
      /// </value>
      public Guid? SubordinateId
      {
        get
        {
          return this.subordinateReference
                 ?? (this.subordinateReference = this.GetInputParameter<Guid>(SubordinateIdKey));
        }
      }

      /// <summary>
      /// Gets the entity moniker.
      /// </summary>
      /// <value>
      /// The entity moniker.
      /// </value>
      public EntityReference EntityMoniker
      {
        get
        {
          return this.entityMoniker ?? (this.entityMoniker = this.GetInputParameter<EntityReference>(EntityMonikerKey));
        }
      }

      /// <summary>
      /// Gets the query.
      /// </summary>
      /// <value>
      /// The query.
      /// </value>
      public QueryExpression QueryExpr
      {
        get
        {
          return this.query ?? (this.query = this.GetInputParameter(QueryKey) as QueryExpression);
        }
      }

      /// <summary>
      /// Gets the pre image.
      /// </summary>
      /// <value>
      /// The pre image.
      /// </value>
      public TEntity PreImage
      {
        get
        {
          return this.preImage ?? (this.preImage = this.GetPreImage());
        }
      }

      /// <summary>
      /// Gets the post image.
      /// </summary>
      /// <value>
      /// The post image.
      /// </value>
      public TEntity PostImage
      {
        get
        {
          return this.postImage ?? (this.postImage = this.GetPostImage());
        }
      }

      /// <summary>
      /// Gets the assignee.
      /// </summary>
      /// <value>The assignee.</value>
      public EntityReference Assignee
      {
        get
        {
          return this.assignee ?? (this.assignee = this.GetInputParameter<EntityReference>(AssigneeKey));
        }
      }

      /// <summary>
      /// Gets or sets the content of the update.
      /// </summary>
      /// <value>
      /// The content of the update.
      /// </value>
      public TEntity UpdateContent
      {
        get
        {
          if (this.updateContent == null)
          {
            if (this.UpdateContentEntity != null)
            {
              this.updateContent = this.UpdateContentEntity.ToEntity<TEntity>();
            }
          }

          return this.updateContent;
        }

        set
        {
          this.updateContentEntity = value;
        }
      }

      /// <summary>
      /// Gets or sets the update content entity.
      /// </summary>
      /// <value>
      /// The update content entity.
      /// </value>
      public Entity UpdateContentEntity
      {
        get
        {
          return this.updateContentEntity
                 ?? (this.updateContentEntity = this.GetInputParameter<Entity>(UpdateContentKey));
        }

        set
        {
          this.updateContentEntity = value;
          this.SetInputParameter(UpdateContentKey, value);
        }
      }

      /// <summary>
      /// Gets the organization service factory.
      /// </summary>
      /// <value>
      /// The organization service factory.
      /// </value>
      private IOrganizationServiceFactory OrganizationServiceFactory
      {
        get
        {
          return this.organizationServiceFactory ??
            (this.organizationServiceFactory = (IOrganizationServiceFactory)this.serviceProvider.GetService(typeof(IOrganizationServiceFactory)));
        }
      }

      /// <summary>
      /// Resets the input target entity.
      /// </summary>
      public void ResetInputTargetEntity()
      {
        this.inputTarget = this.InputTargetAsEntity.ToEntity<TEntity>();
      }

      /// <summary>
      /// Determines whether the specified message name contains message.
      /// </summary>
      /// <param name="messageName">
      /// Name of the message.
      /// </param>
      /// <returns>
      /// true in case it contains the message
      /// </returns>
      public bool IsMessage(MessageName messageName)
      {
        return this.ExecContext.MessageName != null && this.ExecContext.MessageName == messageName.ToString();
      }

      /// <summary>
      /// Traces the specified message.
      /// </summary>
      /// <param name="message">
      /// The message.
      /// </param>
      public void Trace(string message)
      {
        if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
        {
          return;
        }

        if (this.ExecContext == null)
        {
          this.TracingService.Trace(message);
        }
        else
        {
          this.TracingService.Trace(
            "{0}, Correlation Id: {1}, Initiating User: {2}", 
            message, 
            this.ExecContext.CorrelationId, 
            this.ExecContext.InitiatingUserId);
        }
      }

      /// <summary>
      /// Traces the specified message.
      /// </summary>
      /// <param name="message">
      /// The message.
      /// </param>
      /// <param name="args">
      /// The arguments.
      /// </param>
      public void Trace(string message, params object[] args)
      {
        this.Trace(string.Format(message, args));
      }

      /// <summary>
      /// Gets the pre image entity.
      /// </summary>
      /// <param name="imageName">
      /// Name of the image.
      /// </param>
      /// <returns>
      /// Pre Image Entity
      /// </returns>
      public Entity GetPreImageEntity(string imageName = null)
      {
        imageName = imageName ?? DefaultPreImageName;
        return this.GetImageEntity(imageName, true);
      }

      /// <summary>
      /// Gets the pre image.
      /// </summary>
      /// <param name="imageName">
      /// Name of the image.
      /// </param>
      /// <returns>
      /// Pre Image
      /// </returns>
      public TEntity GetPreImage(string imageName = null)
      {
        var entity = this.GetPreImageEntity(imageName);
        return entity == null ? null : entity.ToEntity<TEntity>();
      }

      /// <summary>
      /// Gets the post image entity.
      /// </summary>
      /// <param name="imageName">
      /// Name of the image.
      /// </param>
      /// <returns>
      /// Post Image Entity
      /// </returns>
      public Entity GetPostImageEntity(string imageName = null)
      {
        imageName = imageName ?? DefaultPostImageName;
        return this.GetImageEntity(imageName, false);
      }

      /// <summary>
      /// Gets the post image.
      /// </summary>
      /// <param name="imageName">
      /// Name of the image.
      /// </param>
      /// <returns>
      /// Post Image
      /// </returns>
      public TEntity GetPostImage(string imageName = null)
      {
        var entity = this.GetPostImageEntity(imageName);
        return entity == null ? null : entity.ToEntity<TEntity>();
      }

      /// <summary>
      /// Gets the shared variable.
      /// </summary>
      /// <param name="name">
      /// The name.
      /// </param>
      /// <param name="useParentContext">
      /// if set to <c>true</c> [use parent context].
      /// </param>
      /// <remarks>
      /// Use <c>useParentContext</c> = true for the create, update, delete plug-in registered in 20 or 40, to get the shared variables from a stage 10 
      /// </remarks>
      /// <returns>
      /// Shared Variable value
      /// </returns>
      public object GetSharedVariable(string name, bool useParentContext = false)
      {
        var context = useParentContext ? this.ExecContext.ParentContext : this.ExecContext;
        var vars = context.SharedVariables;
        return vars.Contains(name) ? vars[name] : null;
      }

      /// <summary>
      /// Gets the shared variable.
      /// </summary>
      /// <typeparam name="T">
      /// Variable content type
      /// </typeparam>
      /// <param name="name">
      /// The name.
      /// </param>
      /// <param name="useParentContext">
      /// if set to <c>true</c> [use parent context].
      /// </param>
      /// <returns>
      /// Shared Variable value
      /// </returns>
      /// <remarks>
      /// Use <c>useParentContext</c> = true for the create, update, delete plug-in registered in 20 or 40, to get the shared variables from a stage 10 
      /// </remarks>
      public T GetSharedVariable<T>(string name, bool useParentContext = false)
      {
        var value = this.GetSharedVariable(name, useParentContext);
        if (value == null)
        {
          return default(T);
        }

        return (T)value;
      }

      /// <summary>
      /// Sets the shared variable.
      /// </summary>
      /// <param name="name">
      /// The name.
      /// </param>
      /// <param name="value">
      /// The value.
      /// </param>
      public void SetSharedVariable(string name, object value)
      {
        var vars = this.ExecContext.SharedVariables;
        if (vars.Contains(name))
        {
          vars[name] = value;
        }
        else
        {
          vars.Add(name, value);
        }
      }

      /// <summary>
      /// Gets the input parameter.
      /// </summary>
      /// <typeparam name="TK">The type of the TK.</typeparam>
      /// <param name="key">The key.</param>
      /// <returns>
      /// An instance of the TK
      /// </returns>
      public TK GetInputParameter<TK>(string key)
      {
        var parameters = this.ExecContext.InputParameters;
        return this.GetParameter<TK>(parameters, key);
      }

      /// <summary>
      /// Gets the input parameter.
      /// </summary>
      /// <param name="key">The key.</param>
      /// <returns>
      /// Parameter value
      /// </returns>
      public object GetInputParameter(string key)
      {
        var parameters = this.ExecContext.InputParameters;
        return this.GetParameter(parameters, key);
      }

      /// <summary>
      /// Gets the output parameter.
      /// </summary>
      /// <typeparam name="TK">The type of the TK.</typeparam>
      /// <param name="key">The key.</param>
      /// <returns>
      /// An instance of the TK
      /// </returns>
      public TK GetOutputParameter<TK>(string key)
      {
        var parameters = this.ExecContext.OutputParameters;
        return this.GetParameter<TK>(parameters, key);
      }

      /// <summary>
      /// Gets the parameter.
      /// </summary>
      /// <typeparam name="TK">The type of the TK.</typeparam>
      /// <param name="parameters">The parameters.</param>
      /// <param name="key">The key.</param>
      /// <returns>
      /// An instance of the TK
      /// </returns>
      public TK GetParameter<TK>(ParameterCollection parameters, string key)
      {
        if (!parameters.ContainsKey(key))
        {
          return default(TK);
        }

        return (TK)parameters[key];
      }

      /// <summary>
      /// Gets the parameter.
      /// </summary>
      /// <param name="parameters">The parameters.</param>
      /// <param name="key">The key.</param>
      /// <returns>
      /// Parameter value
      /// </returns>
      public object GetParameter(ParameterCollection parameters, string key)
      {
        return !parameters.ContainsKey(key) ? null : parameters[key];
      }

      /// <summary>
      /// Sets the input parameter.
      /// </summary>
      /// <param name="key">The key.</param>
      /// <param name="value">The value.</param>
      public void SetInputParameter(string key, object value)
      {
        this.SetParameter(this.executionContext.InputParameters, key, value);
      }

      /// <summary>
      /// Sets the output parameter.
      /// </summary>
      /// <param name="key">The key.</param>
      /// <param name="value">The value.</param>
      public void SetOutputParameter(string key, object value)
      {
        this.SetParameter(this.executionContext.OutputParameters, key, value);
      }

      /// <summary>
      /// Sets the parameter.
      /// </summary>
      /// <param name="parameters">The parameters.</param>
      /// <param name="key">The key.</param>
      /// <param name="value">The value.</param>
      public void SetParameter(ParameterCollection parameters, string key, object value)
      {
        parameters[key] = value;
      }

      /// <summary>
      /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
      /// </summary>
      public void Dispose()
      {
        this.Dispose(true);
        GC.SuppressFinalize(this);
      }

      /// <summary>
      /// Releases unmanaged and - optionally - managed resources.
      /// </summary>
      /// <param name="disposing">
      /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
      /// </param>
      protected virtual void Dispose(bool disposing)
      {
        if (this.disposed)
        {
          return;
        }

        if (disposing)
        {
          if (this.orgCtx != null)
          {
            this.orgCtx.Dispose();
          }

          if (this.orgCtxAsSystemUser != null)
          {
            this.orgCtxAsSystemUser.Dispose();
          }
        }

        this.disposed = true;
      }

      /// <summary>
      /// Gets the entity image.
      /// </summary>
      /// <param name="imageName">Name of the image.</param>
      /// <param name="preEntity">if set to <c>true</c> then it's PreEntity, otherwise it's PostImage.</param>
      /// <returns>
      /// The entity image.
      /// </returns>
      private Entity GetImageEntity(string imageName, bool preEntity)
      {
        var images =
          preEntity ?
          this.ExecContext.PreEntityImages :
          this.ExecContext.PostEntityImages;

        if (images.Contains(imageName) && images[imageName] != null)
        {
          return images[imageName];
        }

        return null;
      }
    }

    /// <summary>
    /// Defines the base Plugin Step class
    /// <remarks>
    /// Use it to register step event in the derived plugin class
    /// </remarks>
    /// </summary>
    public class PluginStepBase
    {
      /// <summary>
      /// Gets or sets the pipeline stage.
      /// </summary>
      /// <value>
      /// The pipeline stage.
      /// </value>
      public Stage? Stage { get; set; }

      /// <summary>
      /// Gets or sets the execution mode.
      /// </summary>
      /// <value>
      /// The mode.
      /// </value>
      public Mode? Mode { get; set; }

      /// <summary>
      /// Gets or sets the name of the message.
      /// </summary>
      /// <value>
      /// The name of the message.
      /// </value>
      public MessageName? MessageName { get; set; }

      /// <summary>
      /// Gets or sets the action that fires for the event.
      /// </summary>
      /// <value>
      /// The handler.
      /// </value>
      public Action<PluginContext> Handler { get; set; }
    }
  }
}