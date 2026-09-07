using LMSFinal.Contracts.DTOs.Certificates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LMSFinal.Application.Interfaces
{


    public interface ICertificatePdfGenerator
    {
        byte[] Generate(CertificateDto certificate);
    }
}
