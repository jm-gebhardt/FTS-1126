using System.Collections.ObjectModel;
using AutoMapper;
using Gebhardt.Acc.TestPlanRunner.Domain.Models;
using Gebhardt.Acc.TestPlanRunner.WebApi.Dtos;
using JetBrains.Annotations;

namespace Gebhardt.Acc.TestPlanRunner.WebApi.Profiles;

file sealed class NullIfEmptyConverter : IValueConverter<Collection<string>?, Collection<string>?>
{
    public Collection<string>? Convert(Collection<string>? sourceMember, ResolutionContext context)
        => sourceMember is { Count: > 0 } ? sourceMember : null;
}

/// <summary>
/// AutoMapper profile for mapping between <see cref="TestPlan"/> / <see cref="TestPlanStep"/> and their Dtos.
/// </summary>
[UsedImplicitly]
public class TestPlanProfile : Profile
{
    /// <inheritdoc />
    public TestPlanProfile()
    {
        CreateMap<TestPlanStep, TestPlanStepDto>();
        CreateMap<TestPlanStepDto, TestPlanStep>();

        CreateMap<TestPlan, TestPlanDto>()
            .ForMember(dest => dest.AgvSerialNumbers, opt =>
            {
                opt.AllowNull();
                opt.ConvertUsing(new NullIfEmptyConverter(), src => src.AgvSerialNumbers);
            })
            .ForMember(dest => dest.Steps,
                opt => opt.MapFrom(src => src.Steps))
            .ForSourceMember(src => src.AppliesToAllAgvs, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.HasInfiniteIterations, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.AgvCorrelationIds, opt => opt.DoNotValidate());

        CreateMap<TestPlanDto, TestPlan>()
            .ForMember(dest => dest.AgvSerialNumbers, opt =>
            {
                opt.AllowNull();
                opt.ConvertUsing(new NullIfEmptyConverter(), src => src.AgvSerialNumbers);
            });
    }
}