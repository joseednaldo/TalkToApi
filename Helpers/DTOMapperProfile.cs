using AutoMapper;
using System.Collections.Generic;
using TalkToApi.V1.Models;
using TalkToApi.V1.Models.DTO;

namespace TalkToApi.Helpers
{
    public class DTOMapperProfile : Profile
    {
        public DTOMapperProfile()
        {

            /*
               AutoMapper = Objetivo de converte de um objeto pra outro.
             */
            //Convertendo  "aplicationuser" para o objeto "usuarioDTO"
            CreateMap<ApplicationUser, UsuarioDTO>()
                .ForMember(dest => dest.Nome, orig => orig.MapFrom(srv => srv.FullName));  //Mapeando nomes que são diferentes entre as classes...

            CreateMap<Mensagem, MensagemDTO>();
            CreateMap<ApplicationUser, UsuarioDTOSemHyperLink>()
                .ForMember(dest => dest.Nome, orig => orig.MapFrom(srv => srv.FullName));

        }
    }
}
