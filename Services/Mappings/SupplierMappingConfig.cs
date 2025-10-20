using Domain.Common.Enums;
using Domain.Entities;
using Mapster;
using Shared.DTOs.Suppliers;


public class SupplierMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        

        config.NewConfig<CreateSupplierDto, Supplier>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.VatNumber, src => src.VatNumber)
            .Map(dest => dest.PaymentTerms, src => src.PaymentTerms.Adapt<PaymentTerms>())
            .Ignore(dest => dest.Menus) 
            .ConstructUsing(src =>
                Supplier.Create(
                    src.Name,
                    src.VatNumber,
                    src.PaymentTerms.Adapt<PaymentTerms>(),
                    src.Menus.Adapt<List<Menu>>()
                )
            );


        config.NewConfig<SupplierDto, Supplier>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.VatNumber, src => src.VatNumber)
            .Map(dest => dest.PaymentTerms, src => src.PaymentTerms.Adapt<PaymentTerms>())
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Phone, src => src.Phone)
            .Map(dest => dest.StreetAddress, src => src.StreetAddress)
            .Map(dest => dest.City, src => src.City)
            .Map(dest => dest.PostalCode, src => src.PostalCode)
            .Map(dest => dest.Country, src => src.Country)
            .Map(dest => dest.Menus, src => src.Menus.Adapt<List<Menu>>());


        config.NewConfig<UpdateSupplierDto, Supplier>()
            .Ignore(dest => dest.Menus) // handled separately
            .AfterMapping((src, dest) =>
            {
                dest.GetType().GetProperty(nameof(Supplier.Name))?.SetValue(dest, src.Name);
                dest.GetType().GetProperty(nameof(Supplier.VatNumber))?.SetValue(dest, src.VatNumber);
                dest.GetType().GetProperty(nameof(Supplier.PaymentTerms))?.SetValue(dest, src.PaymentTerms.Adapt<PaymentTerms>());
                dest.GetType().GetProperty(nameof(Supplier.Email))?.SetValue(dest, src.Email);
                dest.GetType().GetProperty(nameof(Supplier.Phone))?.SetValue(dest, src.Phone);
                dest.GetType().GetProperty(nameof(Supplier.StreetAddress))?.SetValue(dest, src.StreetAddress);
                dest.GetType().GetProperty(nameof(Supplier.City))?.SetValue(dest, src.City);
                dest.GetType().GetProperty(nameof(Supplier.PostalCode))?.SetValue(dest, src.PostalCode);
                dest.GetType().GetProperty(nameof(Supplier.Country))?.SetValue(dest, src.Country);
            });
    }
}
