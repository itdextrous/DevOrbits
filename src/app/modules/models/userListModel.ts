export class UserList {
    userId: string = '';
    nameWithRole: string = '';
}

export class AdminTransactions {
    agentBrokerList: Array<UserList> = new Array<UserList>();
    agentPieChart: {} = {};
    elfPieChart: {} = {};
    feedbackMessages: any;
    
    todayActiveUsers: number = 0;
    todayInCompleteTransaction: number = 0;
    todayInProcessTransaction: number = 0;
    todayNewTransaction: number = 0;

    transactionBarChart: number = 0;
    userPieChart: {} = {};
    feedbackList:any;
    feedbackUnreadCount: number = 0;

    inProcessTransactionList:any;
    inCompleteTransactionList:any;
    newTransactionList:any;
}
export class FeedbackMessages{
    id: number = 0;
    comment: string = '';
    domain: string = '';
    email: string = '';
    name: string = '';
    profilePic: string = '';
    readStatus: boolean = false;
}