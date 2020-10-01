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
                //Mapeando nomes que são diferentes entre as classes...
                .ForMember(dest => dest.Nome, orig => orig.MapFrom(srv => srv.FullName));

            CreateMap<Mensagem, MensagemDTO>();

        }
    }
}
