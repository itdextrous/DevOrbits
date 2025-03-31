import { Button, Col, Row } from "react-bootstrap";
import * as XLSX from "xlsx";
import { useState, useEffect } from "react";
import Select from "react-select";
import stockTypeApi from "Services/StockTypeApi";
import stockApi from "Services/StockApi";
import StockDto from "Models/Stock/StockDto";

export default function UploadStocksCSV() {
  const [columns, setColumns] = useState([]);
  const [data, setData] = useState([]);
  const [selectedOption, setSelectedOption] = useState(null);
  const [dropdownData, setDropdownData] = useState([]);

  // handle onChange event of the dropdown
  const handleChange = (e: any) => {
    setSelectedOption(e);
  };

  const handleFileUpload = (e: any) => {
    const file = e.target.files[0];
    const reader = new FileReader();
    reader.onload = (evt: any) => {
      const bstr = evt.target.result;
      const wb = XLSX.read(bstr, { type: "binary" });
      const wsname = wb.SheetNames[0];
      const ws = wb.Sheets[wsname];
      const csvData = XLSX.utils.sheet_to_csv(ws);
      processCSV(csvData);
    };
    reader.readAsBinaryString(file);
    console.log(columns);
  };
  const processCSV = (str: any, delim = ",") => {
    const headers = str.slice(0, str.indexOf("\n")).split(delim); 
    const body = str.slice(str.indexOf("\n") + 1);
    const rowsReturned = body.split("\n");
    const records: any = [];
    rowsReturned.forEach((row: any) => {
      if (row === "") {
        return false;
      }
      const outRow = row.replace(/,,/g, ", ,").replace(/,,/g, ", ,").replace(/,,/g, ", ,");
      const arr = outRow.match(/(".*?"|[^",\s]+)(?=\s*,|\s*$)| ,/g);
      const obj: any = {};
      headers.forEach((header: any, i: any) => {
        if (header === "IsUsed" || header === "IsDemo" || header === "IsMiles") {
          obj[header] = arr[i]?.toLowerCase() === "true";
        } else if (header.toLowerCase() === "seats" || header === "Doors" || header === "StockNumber" || header === "YearGroup"
          || header === "Odometer" || header === "GCM" || header === "GVM" || header === "Tare" || header === "SleepingCapacity"
          || header === "Seats" || header === "Doors") {
          obj[header] = Number(arr[i]);
        } else if (header.toLowerCase() === "dapprice" || header.toLowerCase() === "egcprice") {
          obj[header] = parseFloat(arr[i]);
        } else if (header.toLowerCase() === "regexpiry") {
          obj[header] = arr[i] !== "" ? new Date(arr[i]) : null;
        } else {
          obj[header] = arr[i].replace(" ,", "");
        }

        return obj;
      }, {});
      records.push(obj);
      return null;
    });
    setData(records);
    setColumns(headers);
  };

  const onClickSave = (saveStocks: StockDto[]) => {
    stockApi.uploadStocks(saveStocks)
      .then((x: any) => console.log(x))
      .catch((x: any) => console.log(x));
  };
  useEffect(() => {
    stockTypeApi.getStockType(null)
      .then((results: any) => {
        const dropdownValue = results.stockTypeList?.map((x: any) => ({
          label: x.stockTypeName,
          value: x.stockTypeId,
        }));
        setDropdownData(dropdownValue);
      });
  }, []);

  return (
    <>
      <Row>
        <Col>
          <Select
            placeholder="Select Option"
            value={selectedOption} // set selected value
            options={dropdownData} // set list of the data
            onChange={handleChange} // assign onChange function
          />
        </Col>
        <input
          type="file"
          accept=".csv,.xlsx,.xls"
          onChange={handleFileUpload}
        />
      </Row>
      {
        data.length > 0 ?
          (
            <table>
              <thead>
                <th>StockNumber</th>
                <th>YardCode</th>
                <th>Make</th>
                <th>Model</th>
                <th>YearGroup</th>
                <th>Series</th>
                <th>Badge</th>
                <th>RedbookCode</th>
                <th>DAPPrice</th>
                <th>EGCPrice</th>
                <th>IsUsed</th>
                <th>IsDemo</th>
                <th>Body</th>
                <th>Odometer</th>
                <th>IsMiles</th>
                <th>Colour</th>
                <th>Vin</th>
                <th>RegoNum</th>
                <th>ShortDescription</th>
                <th>InteriorColour</th>
                <th>RegoState</th>
                <th>RegExpiry</th>
                <th>BuildDate</th>
                <th>ComplianceDate</th>
                <th>StandardFeature</th>
                <th>OptionFeature</th>
                <th>AdvDescription</th>
                <th>NVIC</th>
                <th>GCM</th>
                <th>GVM</th>
                <th>Tare</th>
                <th>SleepingCapacity</th>
                <th>Toilet</th>
                <th>Shower</th>
                <th>AirConditioning</th>
                <th>Fridge</th>
                <th>Stereo</th>
                <th>EngineNumber</th>
                <th>GearCount</th>
                <th>EnginePower</th>
                <th>PowerkW</th>
                <th>Powerhp</th>
                <th>EngineMake</th>
                <th>GPS</th>
                <th>WheelSize</th>
                <th>TowballWeight</th>
                <th>Warranty</th>
                <th>Wheels</th>
                <th>AxleConfiguration</th>
                <th>Cylinders</th>
                <th>EngineSize</th>
                <th>FuelType</th>
                <th>Transmission</th>
                <th>SpecialPrice</th>
                <th>StockType</th>
                <th>Drive</th>
                <th>Seats</th>
                <th>Doors</th>
                <th>ImportSource</th>
              </thead>
              <tbody>
                {
                  data?.map((item: any) => (
                    <tr>
                      <td>{item.StockNumber}</td>
                      <td>{item.YardCode}</td>
                      <td>{item.Make}</td>
                      <td>{item.Model}</td>
                      <td>{item.YearGroup}</td>
                      <td>{item.Series}</td>
                      <td>{item.Badge}</td>
                      <td>{item.RedbookCode}</td>
                      <td>{item.DAPPrice}</td>
                      <td>{item.EGCPrice}</td>
                      <td>{item.IsUsed}</td>
                      <td>{item.IsDemo}</td>
                      <td>{item.Body}</td>
                      <td>{item.Odometer}</td>
                      <td>{item.IsMiles}</td>
                      <td>{item.Colour}</td>
                      <td>{item.Vin}</td>
                      <td>{item.RegoNum}</td>
                      <td>{item.ShortDescription}</td>
                      <td>{item.InteriorColour}</td>
                      <td>{item.RegoState}</td>
                      <td>{item.RegExpiry.toDateString()}</td>
                      <td>{item.BuildDate}</td>
                      <td>{item.ComplianceDate}</td>
                      <td>{item.StandardFeature}</td>
                      <td>{item.OptionFeature}</td>
                      <td>{item.AdvDescription}</td>
                      <td>{item.NVIC}</td>
                      <td>{item.GCM}</td>
                      <td>{item.GVM}</td>
                      <td>{item.Tare}</td>
                      <td>{item.SleepingCapacity}</td>
                      <td>{item.Toilet}</td>
                      <td>{item.Shower}</td>
                      <td>{item.AirConditioning}</td>
                      <td>{item.Fridge}</td>
                      <td>{item.Stereo}</td>
                      <td>{item.EngineNumber}</td>
                      <td>{item.GearCount}</td>
                      <td>{item.EnginePower}</td>
                      <td>{item.PowerkW}</td>
                      <td>{item.Powerhp}</td>
                      <td>{item.EngineMake}</td>
                      <td>{item.GPS}</td>
                      <td>{item.WheelSize}</td>
                      <td>{item.TowballWeight}</td>
                      <td>{item.Warranty}</td>
                      <td>{item.Wheels}</td>
                      <td>{item.AxleConfiguration}</td>
                      <td>{item.Cylinders}</td>
                      <td>{item.EngineSize}</td>
                      <td>{item.FuelType}</td>
                      <td>{item.Transmission}</td>
                      <td>{item.SpecialPrice}</td>
                      <td>{item.StockType}</td>
                      <td>{item.Drive}</td>
                      <td>{item.Seats}</td>
                      <td>{item.Doors}</td>
                      <td>{item.ImportSource}</td>
                    </tr>
                  ))
                }
              </tbody>
            </table>)
          : null
      }
      <Col md className="text-right">
        {
          data.length !== 0 ? <Button variant="primary" type="submit" onClick={() => onClickSave(data)}>Save</Button>
            : null
        }
      </Col>
    </>
  );
}
