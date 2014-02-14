//necesary Libreries to build the service
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.Serialization;

namespace servicioGeoCodificacionDijin
{
    public partial class _default : System.Web.UI.Page
    {
        // Metod to obtain all id of the service layers
        public String getIdLayers() {

            // difine to address of the service
            String url2 = "http://srvsigmap.policia.gov.co/ArcGIS/rest/services/DIJIN/SIEDCO/MapServer?f=json&pretty=true";
            String data2;

            data2 = new System.Net.WebClient().DownloadString(url2);
            JObject jObj2 = JObject.Parse(data2);
            int i = 0;
            String sIds = "";

            // all over layers into the service and to get the id of the layers with 
            for (i = 0; i < jObj2["layers"].Count(); i++)
            {
                if (jObj2["layers"][i]["name"].ToString() == "ADMIN_GEO.Municipios")
                {
                    sIds = jObj2["layers"][i]["id"].ToString() + ",";
                }
                if (jObj2["layers"][i]["name"].ToString() == "ADMIN_GEO.Barrios")
                {
                    sIds += jObj2["layers"][i]["id"].ToString() + ",";
                }
                if (jObj2["layers"][i]["name"].ToString() == "ADMIN_GEO.JurisdiccionesEstaciones")
                {
                    sIds += jObj2["layers"][i]["id"].ToString() + ",";
                }
                if (jObj2["layers"][i]["name"].ToString() == "cuadrantes")
                {
                    sIds += jObj2["layers"][i]["id"].ToString();
                }
            }
            return sIds;
        }

        // function to load event on the page
        protected void Page_Load(object sender, EventArgs e)
        {
            //Global variables to load event
            string strLatEnt = Request["lat"]!=null?Request["lat"]:"0";
            string strLonEnt = Request["lon"] != null ? Request["lon"] : "0";

            try
             {
                
                double dblLat = convertData(strLatEnt);
                double dblLon = convertData(strLonEnt);
                string strMaxLat = calculateMaxExtend(dblLat);
                string strMaxLon = calculateMaxExtend(dblLon);
                string strMinLat = calculateMinExtend(dblLat);
                string strMinLon = calculateMinExtend(dblLon);
                string strLat = dblLat.ToString();
                string strLon = dblLon.ToString();
                string slayers = getIdLayers();
                strLon = strLon.Replace(",", ".");
                strLat = strLat.Replace(",", ".");
                strMaxLat = strMaxLat.Replace(",", ".");
                strMaxLon = strMaxLon.Replace(",", ".");
                strMinLat = strMinLat.Replace(",", ".");
                strMinLon = strMinLon.Replace(",", ".");

                string strDane = null, strSiedcoEst = null, strBarrio = null, strSiedcoCuad = null, strNumCuad = null;

                String sIdLayers = this.getIdLayers();


                // address to map service
                String url = "http://srvsigmap.policia.gov.co/ArcGIS/rest/services/DIJIN/SIEDCO/MapServer/identify?geometryType=esriGeometryPoint&geometry={'x':" + strLon + ",'y':" + strLat + "}&sr=4326&layers=all:"+slayers+"&time=&layerTimeOptions=&layerdefs=&tolerance=0&mapExtent={'xmin':" + strMinLon + ",'ymin':" + strMinLat + ",'xmax':" + strMaxLon + ",'ymax':" + strMaxLat + "}&imageDisplay=1600,439,96&returnGeometry=false&maxAllowableOffset=&f=JSON";
                String data;

                // get info of service in JSON format
                data = new System.Net.WebClient().DownloadString(url);
                JObject jObj = JObject.Parse(data);
                string jsonValue;


                // get and set the info about SIEDCO, DANE and cuadrantes of the layers  
                if (dblLat != 0.0 && dblLat != 0.0 && jObj["results"].Count() != 0)
                 {
                    string strNameLayer = null;
                    int i = 0;
                    for (i = 0; i < jObj["results"].Count(); i++)
                    {
                        strNameLayer = jObj["results"][i]["layerName"].ToString();
                            if (strNameLayer == "ADMIN_GEO.Municipios")
                            {
                                strDane = jObj["results"][i]["attributes"]["Codigo Municipio"].ToString();
                            }
                            if (strNameLayer == "ADMIN_GEO.Barrios")
                            {
                                strBarrio = jObj["results"][i]["attributes"]["Nombre Barrio"].ToString();
                            }
                            if (strNameLayer == "ADMIN_GEO.JurisdiccionesEstaciones")
                            {
                                strSiedcoEst = jObj["results"][i]["attributes"]["CODIGO_SIEDCO"].ToString();
                            }
                            if (strNameLayer == "cuadrantes")
                            {
                                strSiedcoCuad = jObj["results"][i]["attributes"]["CODIGO_SIEDCO"].ToString();
                                strNumCuad = jObj["results"][i]["attributes"]["NRO_CUADRANTE"].ToString();
                            }
                        }

                        // set the info to JSON format
                        if (strBarrio == null && strSiedcoCuad == null)
                        {

                            jsonValue = @"{""Cod_DANE"" : " + strDane +","+@" ""SIEDCO_Est"": "+strSiedcoEst+@"}";

                            Response.Write(jsonValue);
                        }
                        else
                        {
                            jsonValue = @"{ ""Cod_DANE"": " + strDane + "," + @" ""SIEDCO_Est"": " + strSiedcoEst + "," + @" ""Barrio"":  " + "\"" + strBarrio + "\"" + "," + @" ""SIEDCO_Cuad"": " + strSiedcoCuad + "," + @" ""Num_Cuad"": " + "\""+strNumCuad+"\""+@"}";
                            Response.Write(jsonValue);
                        }
                    }
                    else {
                        jsonValue = @"{
                                       ""results"" : ""Las coordenadas latitud y longitud no existen dentro del mapa.""
                                        }";
                        Response.Write(jsonValue);
                    }
            }

                // control to the exception in the service
            catch (Exception exc){
                Console.Write(""+exc);
            }
        }

        // function to calculate the spatial extend in the query
        public string calculateMaxExtend(double dbl) {
            string strExtend;
            if(dbl < 0){
                    double dblExtend = Math.Abs(dbl) + (Math.Abs(dbl)* 0.0012);
                    dblExtend = -1* dblExtend;
                    strExtend = dblExtend.ToString();
                    
                }else{
                    double dblExtend = dbl - (dbl * 0.0012);
                    strExtend = dblExtend.ToString();
                }
                
            return strExtend;
            
        }


        // function to calculate the minimum extend in the query
        public string calculateMinExtend(double dbl) {
            string strExtend;
            if(dbl < 0){
                double dblExtend = Math.Abs(dbl) - (Math.Abs(dbl) * 0.0012);
                dblExtend = -1 * dblExtend;
                strExtend = dblExtend.ToString();    
            }else{
                double dblExtend = dbl - (dbl * 0.0012);
                strExtend = dblExtend.ToString();
            }
            return strExtend;
        }

        // function to convert le character of the decimal separation to the local decimal separation 
        public double convertData(string str)
        {
            double dblValue;
            string value = "0";
            NumberFormatInfo nfi = CultureInfo.CurrentCulture.NumberFormat;

            string pattern;
            pattern = @"^[+|-]?[0-9]+({0}[0-9])?";
            Regex rgx = new Regex(String.Format(pattern,nfi.NumberDecimalSeparator));
            if (rgx.IsMatch(str))
            {
                value = str;
            }

            try
            {
                dblValue = Convert.ToDouble(value);//4.647996653891401;     
                return dblValue;
            }
            catch (FormatException)
            {
                Console.WriteLine("Imposible convertir el valor: ", str);
                return 0.0;
            }
            catch (OverflowException)
            {
                Console.WriteLine("Valor fuera de rango para uno de tipo double: ", str);
                return 0.0;
            }
        }
    }
}