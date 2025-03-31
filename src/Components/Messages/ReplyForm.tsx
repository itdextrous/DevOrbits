import Icon from "Components/Icon";
import { IConversationMessageRecipientDto } from "Models/Conversation/ConversationDto";
import { MessageRecipientType } from "Models/Message/MessageRecipientType";
import React, { useEffect, useState } from "react";
import { Container, Form, Row, Col, Button } from "react-bootstrap";
import { oidcUserManager } from "Services/Security/OidcUserManager";
import * as XLSX from "xlsx";

export class Recipient {
  name: string;
  id: string;

  constructor(name: string, id: string) {
    this.name = name;
    this.id = id;
  }
}
export interface MessageDetail {
  toId: string ;
  ccId: string | null;
  body: string;
  //attachments: string[];
}

interface Props {
  recipients: IConversationMessageRecipientDto[];
  onSendClick: (message: MessageDetail) => Promise<void>;
  onCancelClick: () => void;
  convoType:Number;
}
let userRole: string;

export default function ReplyForm({ recipients, onSendClick, onCancelClick ,convoType}: Props) {


  let recipientsId = recipients.length !== 0 ? recipients.find((x)=>x.type === MessageRecipientType.To )?.recipientId: '';
   useState(recipientsId || "");
  const [body, setBody] = useState("");
  const [role,setRole]=useState("");
  const [selectedFile, setSelectedFile] = useState([]);
  const getBase64 = (file: any) => {
    return new Promise(resolve => {
      let baseURL;
      // Make new FileReader
      let reader = new FileReader();
      // Convert the file to base64 text
      reader.readAsDataURL(file);
      // on reader load somthing....
      reader.onload = () => {
        // Make a fileInfo Object
        baseURL = reader.result;
        resolve(baseURL);
      };
    });
  };
  async function refreshUser() {
    const updatedUser = await oidcUserManager.getUser();
    userRole = updatedUser?.profile.role;
    setRole(userRole)
  }
  useEffect(()=>{
    refreshUser();
  },[]);
  async function handleSendClick() {
    debugger
let recpiet = convoType === 2 ? 
role === "Broker"  && recipients.length > 0 ? 
 recipients.find((x)=>x.role === "Customer" ) :
 role === "Customer" ?
 (recipients.find((x)=>x.role === "Dealer" )) :
 recipients.find((x)=>x.role === "Broker" || x.role === "Customer" )  :
 role === "Broker"  && recipients.length > 0 ? 
 recipients.find((x)=>x.role === "Customer" ) :
 (recipients.find((x)=>x.role === "Broker" ))  
 ;

let toId = recpiet?.recipientId || "" ;
    await onSendClick({
      toId,
      ccId: null,
      body,
      //   attachments: selectedFile,
    });
    console.log(selectedFile);
    setBody("");
     // var element = document.getElementById("uploadInput")as HTMLButtonElement
    //  element.value = "";
  }
  const handleFileInput = (e: any) => {
    let file = e.target.files[0];
    if (file?.type?.match('image.*')) {
      setSelectedFile(e.target.files[0]);
      getBase64(e.target.files[0])
        .then((result: any) => { setSelectedFile(result); })
        .catch(err => { console.log(err); });
    }
    else {
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
    }
  }
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
          obj[header] = arr[i]?.replace(" ,", "");
        }
        return obj;
      }, {});
      records.push(obj);
      setSelectedFile(records);
      return null;
    });
  };
  return (
    <Container style={{ paddingTop: "1rem" }}>
      <Form className="chat-form">
        <Row>
          <Col md="auto" id="toRecipient">To:
            <label style={{ color: "#fff", backgroundColor: "#0069d9", borderColor: "#0062cc", borderRadius: "4px", padding: "5px", marginLeft: "5px" }}>
              {convoType === 2 ?
                   role === "Customer" ?  recipients.filter((x) => x.role === "Dealer").map((recipient) => { return recipient.name }) :
                   role === "Broker" ?  recipients.filter((x) => x.role === "Customer").map((recipient) => { return recipient.name }) :
                   recipients.filter((x) => x.role === "Customer"  || x.role === "Broker").map((recipient,index) => { 
                     let filteredRecords=  recipients.filter((x) => x.role === "Customer"  || x.role === "Broker");
                      if(index === (filteredRecords.length -1 ))
                       return `${recipient.name}`
                       else return`${recipient.name}, ` 
                      })
             :
             role === "Customer" ?  recipients.filter((x) => x.role === "Broker").map((recipient) => { return recipient.name }) :
             role === "Broker" ?  recipients.filter((x) => x.role === "Customer").map((recipient) => { return recipient.name }) :
null
               }
            </label>
          </Col>
          {
          
          role === "Dealer" || convoType === 1           ?null :
            <Col md="auto">CC: <label style={{ color: "#fff", backgroundColor: "#0069d9", borderColor: "#0062cc", borderRadius: "4px", padding: "5px", marginLeft: "5px" }}>
                  
            {
              role === "Customer" ?  recipients.filter((x) => x.role === "Broker").map((recipient) => { return recipient.name }) :
              role === "Broker" ?  recipients.filter((x) => x.role === "Dealer").map((recipient) => { return recipient.name }) :
         null
          
            }
            </label>
            </Col>
             }
          <Col />
        </Row>
        <Row>
          <Col>
            <textarea
              className="form-control"
              placeholder="Your message"
              rows={6}
              value={body}
              onChange={(e) => { setBody(e.target.value); }}
            />
          </Col>
        </Row>
        <Row style={{ paddingTop: "1rem" }}>
          <Col md="auto">
            <Button
              variant="primary"
              onClick={(e) => {
                e.preventDefault();
                handleSendClick();
              }}
            >Send
            </Button>
            {" "}
            <Button variant="outline-secondary"><Icon icon="paperclip" />Attach</Button>
            <input id="uploadInput" type="file" name="myFiles" onChange={handleFileInput}
            />
          </Col>
          <Col md className="text-right">
            <Button variant="outline-secondary" onClick={() => onCancelClick()}>Cancel</Button>
          </Col>
        </Row>
      </Form>
    </Container>
  );
}
