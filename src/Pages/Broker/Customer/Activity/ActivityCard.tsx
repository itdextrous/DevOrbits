import React from 'react'
import { Row } from 'react-bootstrap'
import ActivityDetails from './ActivityDetails'
import AvtarCard from './AvtarCard'

function ActivityCard() {
    return (
        <>
            <Row style={{border:"1px solid rgba(0,0,0,.125)",borderRadius:".25rem",margin:"0px", height: 80}}>
                <AvtarCard />
                <ActivityDetails />
            </Row>
        </>
    )
}

export default ActivityCard
