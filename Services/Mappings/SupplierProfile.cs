using AutoMapper;
using Domain.Common.Enums;
using Domain.Entities;
using Shared.DTOs.Suppliers;

namespace Services.Mappings;

public class SupplierProfile : Profile
{
    // public SupplierProfile()
    // {
    //     CreateMap<Supplier, SupplierDto>()
    //         .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value));
    //
    //     CreateMap<CreateSupplierCommand, Supplier>()
    //         .ConstructUsing(src => Supplier.Create(src.Name, src.VatNumber, src.PaymentTerms, src.Menus));
    //     
    //     CreateMap<SupplierRequest, CreateSupplierCommand>();
    //     
    //     CreateMap<SupplierDto, SupplierResponse>();
    //     
    //     CreateMap<SupplierDto, SupplierResponse>();
    // }

    public SupplierProfile()
    {
        // CreateMap<CreateSupplierDto, Supplier>()
        //     .ConstructUsing(dto => Supplier.Create(
        //         dto.Name,
        //         dto.VatNumber,
        //         (PaymentTerms)dto.PaymentTerms,
        //         dto.Menus.Select(x => Menu.Create(new SupplierId(x.SupplierId), DateOnly.FromDateTime(DateTime.Now), new List<Meal>()))
        //     ));
        //
        // CreateMap<UpdateSupplierDto, Supplier>()
        //     .ConstructUsing(dto => new Supplier(
        //         new SupplierId(Guid.Parse(dto.Id)),
        //         dto.Name,
        //         dto.VatNumber,
        //         (PaymentTerms)dto.PaymentTerms,
        //     ))
        //     .ForMember(dest => dest.Menus, opt => opt.Ignore());

        CreateMap<Supplier, SupplierDto>()
            .ForMember(dest => dest.Menus, opt => opt.MapFrom(src => src.Menus));

        CreateMap<SupplierDto, Supplier>()
            .ForMember(dest => dest.Menus, opt => opt.MapFrom(src => src.Menus));

        // DTO → Domain
        // CreateMap<SupplierDto, Supplier>()
        //     .ConstructUsing(dto =>
        //         new Supplier(
        //             new SupplierId(Guid.Parse(dto.Id ?? Guid.NewGuid().ToString())),
        //             dto.Name,
        //             dto.VatNumber,
        //             (PaymentTerms)dto.PaymentTerms))
        //     .ForMember(dest => dest.Id, opt => opt.Ignore()) 
        //     .ForMember(dest => dest.Menus, opt => opt.Ignore()); 
    }
}