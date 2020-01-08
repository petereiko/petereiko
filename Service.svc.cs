using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using ClientLibrary;
using System.Configuration;
using System.Drawing;
using System.IO;
using WaecImageValidation;

namespace WaecService
{
    public class Service : IService
    {

        string IService.SaveUserInfos(UserInfo info)
        {
            //check if the passport is valid

            //1 --- valid pix
            //-2 --- access denied
            //0 --- invalid pix
            //-1 server error
            //string c = "0";
            DAL dal = new DAL();
            try
            {
                using (FacialProxy.FaceServiceClient client = new FacialProxy.FaceServiceClient())
                {
                    string code = ConfigurationSettings.AppSettings["AppCode"].ToString();
                    if (code != "")
                    {
                        int ret = client.DetectFaceFromByte(info.Passport, code);
                        if (ret == 0)
                        {
                            return "-6";
                        }
                    }
                }
            }
            catch
            {
            }
            //check dimension of picture
            if(ValidatePicture(info.Passport)  == false)
            {
                return "-6";
            }
            else
            {
                return dal.SaveUserDetails(info);

            }
           
        }

        string IService.SaveUserInfosV2(UserInfo info)
        {
            //check if the passport is valid

            //1 --- valid pix
            //-2 --- access denied
            //0 --- invalid pix
            //-1 server error
            //string c = "0";

            if (info.App_IsUpdated != true)
            {
                return GetError("-8");
            }
            string code = ConfigurationSettings.AppSettings["AppCode"].ToString();
            DAL dal = new DAL();
            try
            {
                using (FacialProxy.FaceServiceClient client = new FacialProxy.FaceServiceClient())
                {
                    if (code != "")
                    {
                        int ret = client.DetectFaceFromByte(info.Passport, code);
                        if (ret == 0)
                        {
                            return GetError("-7"); 
                        }
                    }
                }
            }
            catch
            {
            }
            //check dimension of picture
            if (ValidatePicture(info.Passport) == false)
            {
                return GetError("-6");
            }

            string description = "";
            var imageResponse = Main.ImageDetection(info.Passport, code);

            StringBuilder sb = new StringBuilder();
            if (imageResponse.Code == "00")
            {
                string response = dal.SaveUserDetails(info);
                return GetError(response);
            }
            else if (imageResponse.History != null)
            {
                int countNoOrError = 0;
                foreach (var item in imageResponse.History)
                {
                    if (item.ValidationResponseCode != "00")
                    {
                        countNoOrError = countNoOrError + 1;
                        var errorDesc = GetError(item.ValidationResponseCode);
                        if (countNoOrError > 1)
                        {
                            var descArray = errorDesc.Split('-');
                            sb.Append(descArray[1] + ",");
                        }
                        else
                        {
                            sb.Append(errorDesc + ",");
                        }
                    }
                }

                description = sb.ToString();
                return description;
            }
            else
            {
                return GetError("10");
            }

            

        }

        private string GetError(string success)
        {
            // retVal  = 1 ==> successfull
            // retVal  = -2 ==> invalid username
            // retval  = -3 ==> registration closed
            // retval  = -4 ==> low quality
            // retval  = -5 ==> server error
            // retval  = -6 ==> bad passport
            string ret = "";
            if (success == "00")
            {
                ret = "1_Succesful.\n";
            }
            else if (success == "01")
            {
                ret = "01_Eyes not visible.\n";
            }
            else if (success == "02")
            {
                ret = "02_Face not found or facial recognition quality is too low.\n";
            }
            else if (success == "05")
            {
                ret = "05_Wrong Dimension. Upload passport with dimension 275(W) X 314(H) pixels\n";
            }
            else if (success == "06")
            {
                ret = "06_Blur Image\n";
            }
            else if (success == "07")
            {
                ret = "07_invalid image size. Upload passport with dimension 275(W) X 314(H) pixels\n";
            }
            else if (success == "08")
            {
                ret = "08_invalid image format.\n";
            }
            else if (success == "09")
            {
                ret = "08_invalid Head Tilt.\n";
            }
            else if (success == "10")
            {
                ret = "-4_Verification Failed\n";
            }
            else if (success == "11")
            {
                ret = "11_invalid Image background\n";
            }

            else if (success == "1")
            {
                ret = "1_Succesful.\n";
            }
            else if (success == "-2")
            {
                ret = "-2_Specified username already exists.\n";
            }
            else if (success == "-3")
            {
                ret = "-3_Registration has closed.\n";
            }

            else if (success == "-4")
            {
                ret = "-4_Fingerprint quality is very low\n";

            }
            else if (success == "-5")
            {
                ret = "-5_The server is busy at the moment\n";
            }
            else if (success == "-6")
            {
                ret = "-6_Invalid passport dimension. Upload passport with dimension 275(W) X 314(H) pixels\n";
            }
            else if (success == "-7")
            {
                ret = "-7_Facial recognition quality is too low; also make sure your eyes, ears, nose and mouth are visible.\n";
            }
            else if (success == "-8")
            {
                ret = "-8_Application is not updated. Kindly download the updated version";
            }
            return ret;
        }

        bool IService.IsValidUserName(string username)
        {
            DAL dal = new DAL();
            return dal.IsValidUserName(username);
        }


        private Image GetImageFromByte(byte[] passport)
        {
            using (MemoryStream mStream = new MemoryStream(passport))
            {
                return Image.FromStream(mStream);
            }
        }
        private bool ValidatePicture(byte[] passport)
        {
            bool result = false;
            Image image = GetImageFromByte(passport);
            if (image.Width >= 275 && image.Height >= 314)
            {
                result = true;
            }
            else
            {
                result = false;
            }
               
            //check file type
           

            return result;
        }
    }
}
