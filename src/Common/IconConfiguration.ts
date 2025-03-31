import { library } from "@fortawesome/fontawesome-svg-core";
// import { fas } from "@fortawesome/free-solid-svg-icons";
import {
  faBars, faPlus, faCheckCircle, faExclamationTriangle, faExclamationCircle, faTimesCircle,
  faPaperclip, faEnvelope, faUserCheck, faClock, faEdit,
} from "@fortawesome/free-solid-svg-icons";

// https://fontawesome.com/how-to-use/on-the-web/using-with/react

export default function configureIconLibrary() {
  // Import all solid icons (big bundle size tho)
  // library.add(fas);

  // Import only some icons - keeps bundle small
  library.add(faBars, faPlus, faCheckCircle, faExclamationTriangle, faExclamationCircle, faTimesCircle,
    faPaperclip, faEnvelope, faUserCheck, faClock, faEdit);
}
