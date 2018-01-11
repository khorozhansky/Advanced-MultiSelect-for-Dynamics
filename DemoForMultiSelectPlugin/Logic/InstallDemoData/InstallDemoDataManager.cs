namespace DemoForAdvancedMultiSelectPlugin.Logic.InstallDemoData
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;

  using AdvancedMultiSelect;

  using Microsoft.Xrm.Sdk;
  using Microsoft.Xrm.Sdk.Messages;

  using AdvancedMultiSelect.CrmProxy;
  using AdvancedMultiSelect.Logic;

  public class InstallDemoDataManager : ManagerBase<Entity>
  {
    public InstallDemoDataManager(PluginBase<Entity>.PluginContext pluginContext)
      : base(pluginContext)
    {
    }

    public void InstallData()
    {
      var pluginContext = this.PluginContext;
      var install = this.PluginContext.GetInputParameter<bool?>("InstallDemoData") ?? false;
      bool demoDataExist;
      if (install)
      {
        this.UpsertItemSetConfigurationData();
        this.UpsertDemoData();
        demoDataExist = true;
      }
      else
      {
        var orgCtx = pluginContext.OrgCtx;
        demoDataExist = orgCtx.pavelkh_InterestSet
          .Select(r => r.pavelkh_InterestId)
          .FirstOrDefault() != null;
      }

      this.PluginContext.SetOutputParameter("DemoDataExists", demoDataExist);
    }

    private void UpsertItemSetConfigurationData()
    {
      var pluginContext = this.PluginContext;
      var baseConfigDataWebResourceId = new Guid("CBD99D59-A2DD-E711-81A8-08002703F176");
      var configurationContent = 
        pluginContext.OrgCtx.WebResourceSet
        .Where(r => r.WebResourceId == baseConfigDataWebResourceId)
        .Select(r => r.Content).FirstOrDefault();
      if (configurationContent == null)
      {
        throw new InvalidPluginExecutionException("'[tuneXrm] Base Demo Item Set Configuration' webresource does not contain data.");
      }

      var configuratonBinary = Convert.FromBase64String(configurationContent);
      var configuration = Encoding.UTF8.GetString(configuratonBinary);

      var importConfigurationRequest = new OrganizationRequest("pavelkh_ItemSetConfigurationImport")
                                         {
                                           ["Configuration"] = configuration
      };
      pluginContext.OrgCtx.Execute(importConfigurationRequest);
    }

    private void UpsertDemoData()
    {
      var pluginContext = this.PluginContext;
      var service = pluginContext.Service;
      var execMultipleReqest = new ExecuteMultipleRequest
      {
        Settings = new ExecuteMultipleSettings { ContinueOnError = false, ReturnResponses = false },
        Requests = new OrganizationRequestCollection()
      };

      var interests = this.GetInterestsData();
      foreach (var item in interests)
      {
        execMultipleReqest.Requests.Add(new UpsertRequest { Target = item });
      }

      var marketingLists = this.GetMarketingListData();
      foreach (var item in marketingLists)
      {
        execMultipleReqest.Requests.Add(new CreateRequest { Target = item });
      }

      var executeMultipleResponse = (ExecuteMultipleResponse)service.Execute(execMultipleReqest);
      if (executeMultipleResponse.IsFaulted)
      {
        var fault = executeMultipleResponse.Responses.FirstOrDefault(r => r.Fault != null)?.Fault;
        var errorMessage = fault?.Message + fault?.ErrorDetails.FirstOrDefault().Value;
        throw new InvalidPluginExecutionException($"An error occured while adding demo data. ({errorMessage})");
      }
    }

    private IList<Entity> GetInterestsData()
    {
      var interests = new List<Interest> {
        new Interest("01eeb786-30d8-e711-81a4-08002703f176",686590000,"Stocks"),
        new Interest("03eeb786-30d8-e711-81a4-08002703f176",686590000,"Bonds"),
        new Interest("05eeb786-30d8-e711-81a4-08002703f176",686590000,"Mutual funds"),
        new Interest("07eeb786-30d8-e711-81a4-08002703f176",686590000,"ETFs"),
        new Interest("09eeb786-30d8-e711-81a4-08002703f176",686590000,"Real estate"),
        new Interest("0beeb786-30d8-e711-81a4-08002703f176",686590000,"Hedge funds"),
        new Interest("0deeb786-30d8-e711-81a4-08002703f176",686590000,"Private equity"),
        new Interest("0feeb786-30d8-e711-81a4-08002703f176",686590000,"Other"),
        new Interest("11eeb786-30d8-e711-81a4-08002703f176",686590001,"Solar Energy"),
        new Interest("13eeb786-30d8-e711-81a4-08002703f176",686590001,"Geothermal Power"),
        new Interest("15eeb786-30d8-e711-81a4-08002703f176",686590001,"Hydropower"),
        new Interest("17eeb786-30d8-e711-81a4-08002703f176",686590001,"Wind Energy"),
        new Interest("19eeb786-30d8-e711-81a4-08002703f176",686590001,"Wave Power"),
        new Interest("1beeb786-30d8-e711-81a4-08002703f176",686590001,"Biomass"),
        new Interest("1deeb786-30d8-e711-81a4-08002703f176",686590002,"Action"),
        new Interest("1feeb786-30d8-e711-81a4-08002703f176",686590002,"Adventure"),
        new Interest("21eeb786-30d8-e711-81a4-08002703f176",686590002,"Drama"),
        new Interest("23eeb786-30d8-e711-81a4-08002703f176",686590002,"Comedy"),
        new Interest("25eeb786-30d8-e711-81a4-08002703f176",686590002,"Historical"),
        new Interest("27eeb786-30d8-e711-81a4-08002703f176",686590002,"Musicals"),
        new Interest("29eeb786-30d8-e711-81a4-08002703f176",686590002,"War"),
        new Interest("2beeb786-30d8-e711-81a4-08002703f176",686590002,"Westerns"),
        new Interest("2deeb786-30d8-e711-81a4-08002703f176",686590002,"Horror"),
        new Interest("2feeb786-30d8-e711-81a4-08002703f176",686590002,"Sci-fi"),
        new Interest("31eeb786-30d8-e711-81a4-08002703f176",686590003,"Badminton"),
        new Interest("33eeb786-30d8-e711-81a4-08002703f176",686590003,"Ball badminton"),
        new Interest("35eeb786-30d8-e711-81a4-08002703f176",686590003,"Biribol"),
        new Interest("37eeb786-30d8-e711-81a4-08002703f176",686590003,"Bossaball"),
        new Interest("39eeb786-30d8-e711-81a4-08002703f176",686590003,"Fistball"),
        new Interest("3beeb786-30d8-e711-81a4-08002703f176",686590003,"Footbag net"),
        new Interest("3deeb786-30d8-e711-81a4-08002703f176",686590003,"Football tennis"),
        new Interest("3feeb786-30d8-e711-81a4-08002703f176",686590003,"Footvolley"),
        new Interest("41eeb786-30d8-e711-81a4-08002703f176",686590003,"Hooverball"),
        new Interest("43eeb786-30d8-e711-81a4-08002703f176",686590003,"Jianzi"),
        new Interest("45eeb786-30d8-e711-81a4-08002703f176",686590003,"Padel"),
        new Interest("47eeb786-30d8-e711-81a4-08002703f176",686590003,"Peteca"),
        new Interest("49eeb786-30d8-e711-81a4-08002703f176",686590003,"Pickleball"),
        new Interest("4beeb786-30d8-e711-81a4-08002703f176",686590003,"Platform tennis"),
        new Interest("4deeb786-30d8-e711-81a4-08002703f176",686590003,"Sepak takraw"),
        new Interest("4feeb786-30d8-e711-81a4-08002703f176",686590003,"Sipa"),
        new Interest("51eeb786-30d8-e711-81a4-08002703f176",686590003,"Table tennis")};

      interests.ForEach(r => r.Description = $"Some description for {r.Name} Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor...");
      return interests.Select(r => (Entity)(pavelkh_Interest)r).ToList();
    }

    private IList<Entity> GetMarketingListData()
    {
      var accountTargetedList = new OptionSetValue(1);
      var marketingLists = new List<List> {
        new List { ListName = $"[demo] Some Test ML 01 {DateTime.Now:u}", CreatedFromCode = accountTargetedList },
        new List { ListName = $"[demo] Some Test ML 02 {DateTime.Now:u}", CreatedFromCode = accountTargetedList },
        new List { ListName = $"[demo] Some Test ML A1 {DateTime.Now:u}", CreatedFromCode = accountTargetedList },
        new List { ListName = $"[demo] Some Test ML A2 {DateTime.Now:u}", CreatedFromCode = accountTargetedList },
        new List { ListName = $"[demo] Some Test ML B1 {DateTime.Now:u}", CreatedFromCode = accountTargetedList },
      };

      marketingLists.ForEach(r => r.Description = $"Some description for {r.ListName} cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident");
      return marketingLists.Select(r => (Entity)r).ToList();
    }

    public class Interest
    {
      public Guid Id { get; set; }

      public int TypeId { get; set; }

      public string Name { get; set; }

      public string Description { get; set; }

      public Interest(string id, int typeId, string name)
      {
        this.Id = new Guid(id);
        this.TypeId = typeId;
        this.Name = name;
      }

      public static explicit operator pavelkh_Interest(Interest interest)
      {
        return new pavelkh_Interest
                 {
                   pavelkh_InterestId = interest.Id,
                   pavelkh_Type = new OptionSetValue(interest.TypeId),
                   pavelkh_name = interest.Name,
                   pavelkh_Description = interest.Description
                 };
      }
    }
  }
}